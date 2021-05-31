using System;
using System.Collections.Generic;
using System.Numerics;

namespace Common {
	public static class Helpers {
		public static List<Vector2> Circle(Vector2 origin, float radius, int steps = 32) {
			var points = new List<Vector2>();
			for(var theta = 0f; theta <= MathF.Tau; theta += MathF.Tau / steps)
				points.Add(new Vector2(origin.X + radius * MathF.Cos(theta), origin.Y + radius * MathF.Sin(theta)));
			points.Add(points[0]);
			return points;
		}

		public static float Mix(float a, float b, float t) => (b - a) * t + a;
		public static Vector2 Mix(Vector2 a, Vector2 b, float t) => (b - a) * t + a;
		public static Vector3 Mix(Vector3 a, Vector3 b, float t) => (b - a) * t + a;

		public static Vector2 Apply(this Vector2 v, Func<float, float> t) => new(t(v.X), t(v.Y));
		public static float Apply(this Vector2 v, Func<float, float, float> t) => t(v.X, v.Y);
		public static Vector3 Apply(this Vector3 v, Func<float, float> t) => new(t(v.X), t(v.Y), t(v.Z));
		public static float Apply(this Vector3 v, Func<float, float, float> t) => t(t(v.X, v.Y), v.Z);
		public static (int X, int Y) CeilInt(this Vector2 v) => ((int) MathF.Ceiling(v.X), (int) MathF.Ceiling(v.Y));
		public static void Deconstruct(this Vector2 v, out float x, out float y) => (x, y) = (v.X, v.Y);

		public static Vector2 Normalize(this Vector2 v) => Vector2.Normalize(v);

		public static Vector2 Abs(this Vector2 v) => new(MathF.Abs(v.X), MathF.Abs(v.Y));

		public static Vector2 Max(this Vector2 a, Vector2 b) => new(
			MathF.Max(a.X, b.X),
			MathF.Max(a.Y, b.Y)
		);
		public static Vector2 Min(this Vector2 a, Vector2 b) => new(
			MathF.Min(a.X, b.X),
			MathF.Min(a.Y, b.Y)
		);

		public static Vector2 Rotate(this Vector2 v, float a) {
			var ca = MathF.Cos(a);
			var sa = MathF.Sin(a);
			return new(
				v.X * ca - v.Y * sa, 
				v.X * sa + v.Y * ca
			);
		}
	}
}