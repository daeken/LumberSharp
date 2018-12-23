using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PrettyPrinter;

namespace Renderer {
	public class Mesh {
		public readonly IReadOnlyList<Triangle> Triangles;
		public readonly Material Material;
		public readonly AABB BoundingBox;

		public Mesh(IReadOnlyList<Triangle> triangles, Material material) {
			Triangles = triangles;
			Material = material;
			var bounds = Triangles.Select(x => new[] { x.A, x.B, x.C }).SelectMany(x => x).Bounds();
			BoundingBox = new AABB(bounds.Low, bounds.High - bounds.Low);
		}

		public (Triangle, float)? Intersect(Ray ray) {
			Triangle? closest = null;
			var dist = float.PositiveInfinity;
			foreach(var tri in Triangles) {
				var cdist = tri.Intersect(ray);
				if(cdist == -1 || cdist > dist) continue;
				closest = tri;
				dist = cdist;
			}
			if(closest == null) return null;
			return (closest.Value, dist);
		}
	}
}