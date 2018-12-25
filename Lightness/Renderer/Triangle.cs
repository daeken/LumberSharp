using System.Numerics;

namespace Lightness.Renderer {
	public struct Triangle {
		public readonly Vector3 A, B, C;
		public readonly Vector3 NA, NB, NC;
		
		public Vector3[] AsArray => new[] { A, B, C };

		public Vector3 FaceNormal => Vector3.Cross(B - A, C - A).Normalized();

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
	}
}