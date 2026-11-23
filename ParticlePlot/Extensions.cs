using System;
using System.Numerics;

namespace ParticlePlot {
	public static class Extensions {
		public static Vector2 Rotate(this Vector2 v, float angle) {
			var (sin, cos) = MathF.SinCos(angle);
			return new Vector2(v.X * cos - v.Y * sin, v.Y * cos + v.X * sin);
		}

		public static Vector2 Normalized(this Vector2 v) => Vector2.Normalize(v);
	}
}
