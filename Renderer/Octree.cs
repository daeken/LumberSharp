using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using static System.Console;

namespace Renderer {
	public class Octree {
		readonly Octree[] Nodes;
		readonly Mesh Leaf;
		readonly AABB BoundingBox;
		readonly bool Empty;
		readonly float Diameter;

		public Octree(Mesh mesh, int maxTrisPerLeaf, AABB? boundingBox = null, int depth = 0) {
			BoundingBox = boundingBox/*?.Union(mesh.BoundingBox)*/ ?? mesh.BoundingBox;
			if(BoundingBox.Size == Vector3.Zero) { Empty = true; return; }
			Diameter = BoundingBox.Size.ComponentMax();
			if(mesh.Triangles.Count <= maxTrisPerLeaf) {
				Leaf = mesh;
				if(mesh.Triangles.Count == 0)
					Empty = true;
				BoundingBox = Leaf.BoundingBox;
				Diameter = BoundingBox.Size.ComponentMax();
				return;
			}

			var planes = BoundingBox.MidPlanes;
			var triLists = Enumerable.Range(0, 8).Select(x => new ConcurrentBag<Triangle>()).ToArray();

			void Divide(Triangle tri, int nodeIdx, int planeIdx) {
				if(planeIdx == 3) {
					triLists[nodeIdx].Add(tri);
					return;
				}

				var plane = planes[planeIdx];
				var sideA = Vector3.Dot(tri.A, plane.Normal) - plane.Distance >= -0.0001f;
				var sideB = Vector3.Dot(tri.B, plane.Normal) - plane.Distance >= -0.0001f;
				var sideC = Vector3.Dot(tri.C, plane.Normal) - plane.Distance >= -0.0001f;

				if(sideA == sideB && sideB == sideC)
					Divide(tri, nodeIdx | ((sideA ? 1 : 0) << planeIdx), planeIdx + 1);
				else {
					Divide(tri, nodeIdx, planeIdx + 1);
					Divide(tri, nodeIdx | (1 << planeIdx), planeIdx + 1);
				}
			}
			
			Parallel.ForEach(mesh.Triangles, tri => Divide(tri, 0, 0));
			
			var fc = triLists.First(x => x.Count != 0).Count;
			if(triLists.All(x => x.Count == fc)) {
				WriteLine($"Making premature leaf with {fc} triangles instead of {maxTrisPerLeaf}");
				Leaf = mesh;
				BoundingBox = Leaf.BoundingBox;
				return;
			}
			
			var mm = (BoundingBox.Min, BoundingBox.Center);
			var ms = BoundingBox.Size / 2;
			var boundingMins = new[] {
				mm.Select(0, 0, 0), 
				mm.Select(0, 0, 1), 
				mm.Select(0, 1, 0), 
				mm.Select(0, 1, 1), 
				mm.Select(1, 0, 0), 
				mm.Select(1, 0, 1), 
				mm.Select(1, 1, 0), 
				mm.Select(1, 1, 1)
			};
			
			Nodes = triLists.Select((x, i) => new Octree(new Mesh(x.ToList(), mesh.Material), maxTrisPerLeaf, new AABB(boundingMins[i], ms), depth + 1)).ToArray();
		}

		public (Triangle, Material, Vector3, float)? FindIntersectionSlow(Ray ray) {
			if(Empty || !BoundingBox.IntersectedBy(ray)) return null;
			if(Leaf != null) {
				(Triangle, Material, Vector3, float)? closest = null;
				var distance = float.PositiveInfinity;
				foreach(var triangle in Leaf.Triangles) {
					var hit = triangle.Intersect(ray);
					if(hit != -1 && hit < distance) {
						distance = hit;
						closest = (triangle, Leaf.Material, ray.Origin + ray.Direction * hit, hit);
					}
				}
				return closest;
			}

			(Triangle, Material, Vector3, float)? closestBox = null;
			var boxDist = float.PositiveInfinity;
			foreach(var node in Nodes) {
				if(node.Empty) continue;
				var ret = node.FindIntersectionSlow(ray);
				if(ret != null) {
					var dist = (ret.Value.Item3 - ray.Origin).LengthSquared();
					if(dist < boxDist) {
						closestBox = ret;
						boxDist = dist;
					}
				}
			}
			return closestBox;
		}

		public (Triangle, Material, Vector3, float)? FindIntersectionCustom(Ray ray) {
			return !BoundingBox.Contains(ray.Origin)
				? FindIntersectionSlow(ray)
				: SubIntersectionCustom(ray);
		}

		(Triangle, Material, Vector3, float)? SubIntersectionCustom(Ray ray) {
			if(Empty) return null;
			if(Leaf != null) {
				(Triangle, Material, Vector3, float)? closest = null;
				var distance = float.PositiveInfinity;
				foreach(var triangle in Leaf.Triangles) {
					var hit = triangle.Intersect(ray);

					if(hit != -1 && hit < distance) {
						distance = hit;
						closest = (triangle, Leaf.Material, ray.Origin + ray.Direction * hit, hit);
					}
				}
				return closest;
			}
			
			var planes = BoundingBox.MidPlanes;
			var side = (
				X: Vector3.Dot(ray.Origin, planes[2].Normal) - planes[2].Distance >= 0,
				Y: Vector3.Dot(ray.Origin, planes[1].Normal) - planes[1].Distance >= 0,
				Z: Vector3.Dot(ray.Origin, planes[0].Normal) - planes[0].Distance >= 0
			);
			var xDist = side.X == ray.Direction.X < 0
				? planes[2].RayDistance(ray)
				: float.PositiveInfinity;
			var yDist = side.Y == ray.Direction.Y < 0
				? planes[1].RayDistance(ray)
				: float.PositiveInfinity;
			var zDist = side.Z == ray.Direction.Z < 0
				? planes[0].RayDistance(ray)
				: float.PositiveInfinity;
			for(var i = 0; i < 3; ++i) {
				var idx = (side.Z ? 1 : 0) | (side.Y ? 2 : 0) | (side.X ? 4 : 0);
				var ret = Nodes[idx].SubIntersectionCustom(ray);
				if(ret != null) return ret;

				var minDist = MathF.Min(MathF.Min(xDist, yDist), zDist);
				if(float.IsInfinity(minDist) || minDist > Diameter) return null;
				
				var origin = ray.Origin + ray.Direction * minDist;
				if(!BoundingBox.Contains(origin)) return null;
				if(minDist == xDist) { side.X = !side.X; xDist = float.PositiveInfinity; }
				else if(minDist == yDist) { side.Y = !side.Y; yDist = float.PositiveInfinity; }
				else if(minDist == zDist) { side.Z = !side.Z; zDist = float.PositiveInfinity; }
			}

			return null;
		}
	}
}