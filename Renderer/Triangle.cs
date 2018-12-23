using System.Numerics;

namespace Renderer {
	public struct Triangle {
		public readonly Vector3 A, B, C;
		public readonly Vector3 NA, NB, NC;
		
		public Vector3[] AsArray => new[] { A, B, C };

		public Vector3 Normal => Vector3.Cross(B - A, C - A).Normalized();

		public Triangle(Vector3 a, Vector3 b, Vector3 c, Vector3 na, Vector3 nb, Vector3 nc) {
			A = a;
			B = b;
			C = c;
			NA = na;
			NB = nb;
			NC = nc;
		}

		public Triangle(Vector3 a, Vector3 b, Vector3 c, Vector3 n) : this(a, b, c, n, n, n) {
		}
		
		const float Epsilon = 0.00001f;
		// Moller-Trumbore
		public float Intersect(Ray ray) {
			var edge1 = B - A;
			var edge2 = C - A;
			var h = Vector3.Cross(ray.Direction, edge2);
			var a = Vector3.Dot(edge1, h);
			if(a < Epsilon && a > -Epsilon) return -1; // If it's < Epsilon but > -Epsilon, this is a miss; < -Epsilon means hitting a triangle on the opposite face
			var f = 1 / a;
			var s = ray.Origin - A;
			var u = f * Vector3.Dot(s, h);
			if(u < 0 || u > 1) return -1;
			var q = Vector3.Cross(s, edge1);
			var v = f * Vector3.Dot(ray.Direction, q);
			if(v < 0 || u + v > 1) return -1;
			var t = f * Vector3.Dot(edge2, q);
			if(t <= Epsilon) return -1;
			return t;
		}
	}
}