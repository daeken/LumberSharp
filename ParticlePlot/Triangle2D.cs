using System;
using System.Numerics;

namespace ParticlePlot {
	public class Triangle2D : IComparable {
		public Vector2 A, B, C;
		public float Area;

		public Triangle2D(Vector2 a, Vector2 b, Vector2 c) {
			A = a;
			B = b;
			C = c;
			UpdateArea();
		}

		public void UpdateArea() =>
			Area = MathF.Abs((B.X - A.X) * (C.Y - A.Y) - (C.X - A.X) * (B.Y - A.Y));

		public int CompareTo(object obj) => Area.CompareTo(((Triangle2D) obj).Area);
	}
}