using System;
using System.Numerics;

namespace ParticlePlot {
	public static class Extensions {
		public static Vector2 Rotate(this Vector2 v, float angle) {
			var cf = MathF.Cos(angle);
			var sf = MathF.Sin(angle);
			return new Vector2(v.X * cf - v.Y * sf, v.Y * cf + v.X * sf);
		}

		public static Vector2 Normalized(this Vector2 v) => Vector2.Normalize(v);
	}
}