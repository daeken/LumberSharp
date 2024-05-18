using System.Collections.Concurrent;
using System.Numerics;
using Common;
using DoubleSharp.MathPlus;

namespace Facer;

public class Octree : AABB {
	public readonly Octree[] Children;
	public readonly Triangle3D[] Triangles;

	public Octree(IReadOnlyCollection<Triangle3D> triangles, int maxTrianglesPerLeaf) : this(
		triangles, maxTrianglesPerLeaf,
		triangles.Select(x => x.Points).SelectMany(x => x).Aggregate(Vector3.Min),
		triangles.Select(x => x.Points).SelectMany(x => x).Aggregate(Vector3.Max)
	) {}
	
	Octree(IReadOnlyCollection<Triangle3D> triangles, int maxTrianglesPerLeaf, Vector3 elb, Vector3 eub) : base(elb, eub) {
		if(triangles.Count <= maxTrianglesPerLeaf) {
			Triangles = triangles.ToArray();
			Children = null;
			return;
		}

		IEnumerable<Triangle3D> SplitAlong(IEnumerable<Triangle3D> tris, Func<Vector3, float> func) {
			var normal = func(new(-1, 0, 1)) switch {
				-1 => Vector3.UnitX,
				0 => Vector3.UnitY,
				_ => Vector3.UnitZ
			};
			var dist = func(Center);
			return tris.Select(x => x.Split(normal, dist)).SelectMany(x => x);
		}

		/*triangles = SplitAlong(SplitAlong(SplitAlong(triangles, v => v.X), v => v.Y), v => v.Z)
			.ToList();*/

		var e = Size / 2;
		Octree ClipTriangles(Vector3 min) {
			var max = min + e;
			var aabb = new AABB(min, max);
			//var otris = triangles.Where(x => aabb.Contains(x)).ToList();
			var otris = triangles.Where(x => TriangleIntersectsAABB(x, aabb)).ToList();
			return otris.Count == 0 ? null : new Octree(otris, maxTrianglesPerLeaf, min, max);
		}

		Triangles = null;
		Children = [
			ClipTriangles(Min), // Bottom back left
			ClipTriangles(new(Center.X, Min.Y, Min.Z)), // Bottom back right
			ClipTriangles(new(Min.X, Center.Y, Min.Z)), // Bottom front left
			ClipTriangles(new(Center.X, Center.Y, Min.Z)), // Bottom front right

			ClipTriangles(new(Min.X, Min.Y, Center.Z)), // Top back left
			ClipTriangles(new(Center.X, Min.Y, Center.Z)), // Top back right
			ClipTriangles(new(Min.X, Center.Y, Center.Z)), // Top front left
			ClipTriangles(Center) // Top front right
		];
	}

	public IEnumerable<Triangle3D> AllTriangles {
		get {
			if(Triangles != null)
				foreach(var tri in Triangles)
					yield return tri;
			else
				foreach(var child in Children)
					if(child != null)
						foreach(var tri in child.AllTriangles)
							yield return tri;
		}
	}

	public IEnumerable<(Vector3 A, Vector3 B, Triangle3D Triangle)> FindPlaneIntersections(Vector3 normal, float distance, HashSet<Triangle3D> used = null) {
		used ??= [];
		if(Triangles != null) {
			foreach(var tri in Triangles) {
				var (intersects, a, b) = TrianglePlaneIntersection(normal, distance, tri);
				if(intersects && !used.Contains(tri)) {
					yield return (a, b, tri);
					used.Add(tri);
				}
			}
			yield break;
		}

		//if(!PlaneIntersectsAABB(normal, distance))
		//	yield break;

		foreach(var child in Children)
			if(child != null)
				foreach(var ls in child.FindPlaneIntersections(normal, distance, used))
					yield return ls;
	}

	public bool Intersects(Vector3 origin, Vector3 direction, Triangle3D except = null) {
		if(Triangles != null)
			return Triangles.Any(tri =>
				tri != except && tri.FindIntersection(origin, direction) != null);

		if(!IntersectedBy(origin, direction)) return false;

		return Children.Any(child => child != null && child.Intersects(origin, direction));
	}

	public IEnumerable<Triangle3D> Intersects(Triangle3D itri, Triangle3D except = null) {
		if(Triangles != null)
			foreach(var tri in Triangles)
				if(tri != except && tri.Intersects(itri))
					yield return tri;

		if(!TriangleIntersectsAABB(itri, this)) yield break;

		foreach(var child in Children)
			if(child != null)
				foreach(var elem in child.Intersects(itri, except))
					yield return elem;
	}

	// Determine if a segment intersects with the plane and find the intersection point
	static bool GetSegmentPlaneIntersection(Vector3 normal, float distance, Vector3 P1, Vector3 P2, out Vector3 outP) {
		outP = Vector3.Zero;
		var d1 = Vector3.Dot(normal, P1) + distance;
		var d2 = Vector3.Dot(normal, P2) + distance;

		if(d1 * d2 > 0) // Points are on the same side of the plane
			return false;

		var t = d1 / (d1 - d2); // 'time' of intersection point on the segment
		outP = P1 + t * (P2 - P1);

		return true;
	}

	// Find intersection points of the triangle with the plane
	static (bool Intersects, Vector3 A, Vector3 B) TrianglePlaneIntersection(Vector3 normal, float distance, Triangle3D tri) {
		var a = GetSegmentPlaneIntersection(normal, distance, tri.A, tri.B, out var ia);
		var b = GetSegmentPlaneIntersection(normal, distance, tri.B, tri.C, out var ib);
		var c = GetSegmentPlaneIntersection(normal, distance, tri.C, tri.A, out var ic);

		if(a == b && a == c)
			return (false, Vector3.Zero, Vector3.Zero);
		if(a && b)
			return (true, ia, ib);
		if(a && c)
			return (true, ia, ic);
		if(b && c)
			return (true, ib, ic);

		return (false, Vector3.Zero, Vector3.Zero);
	}

	bool PlaneIntersectsAABB(Vector3 normal, float distance) {
		var e = Size / 2;
		var r = e.X * MathF.Abs(normal.X) + e.Y * MathF.Abs(normal.Y) + e.Z * MathF.Abs(normal.Z);
		var s = Vector3.Dot(normal, Center) - distance;
		return MathF.Abs(s) <= r;
	}
	
	// Test if triangle intersects with AABB
	static bool TriangleIntersectsAABB(Triangle3D triangle, AABB aabb) =>
		aabb.Contains(triangle) || aabb.IntersectedBy(triangle);
}