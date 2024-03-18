using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DoubleSharp.MathPlus;
using SDL2;

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
		public static Vector4 Mix(Vector4 a, Vector4 b, float t) => (b - a) * t + a;

		public static Vector2 Apply(this Vector2 v, Func<float, float> t) => new(t(v.X), t(v.Y));
		public static float Apply(this Vector2 v, Func<float, float, float> t) => t(v.X, v.Y);
		public static Vector3 Apply(this Vector3 v, Func<float, float> t) => new(t(v.X), t(v.Y), t(v.Z));
		public static float Apply(this Vector3 v, Func<float, float, float> t) => t(t(v.X, v.Y), v.Z);
		public static (int X, int Y) CeilInt(this Vector2 v) => ((int) MathF.Ceiling(v.X), (int) MathF.Ceiling(v.Y));

		public static Func<T1, T3> Compose<T1, T2, T3>(this Func<T1, T2> f1, Func<T2, T3> f2) =>
			x => f2(f1(x));

		public static List<U> SelectList<T, U>(this IEnumerable<T> seq, Func<T, U> func) => seq.Select(func).ToList();

		public static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;

		public static Vector2 Intersection(Vector2 a, Vector2 b, Vector2 c, Vector2 d) {
			var r = b - a;
			var s = d - c;
			var t = Cross(c - a, s / Cross(r, s));
			return a + r * t;
		}

		public static IEnumerable<(T First, T Second, T Third)> Batch3<T>(this IEnumerable<T> iter) {
			var l = iter.ToList();
			for(var i = 0; i < l.Count; i += 3)
				yield return(l[i], l[i + 1], l[i + 2]);
		}

		public static SDL.SDL_Point ToPoint(this Vector2 v) => 
			new() { x = (int) MathF.Round(v.X), y = (int) MathF.Round(v.Y) };

		public static Vector2 ToVector(this SDL.SDL_Point p) => new(p.x, p.y);

		public static void DrawLine(this IntPtr renderer, Vector2 a, Vector2 b) =>
			_ = SDL.SDL_RenderDrawLine(
				renderer, 
				(int) MathF.Round(a.X),
				(int) MathF.Round(a.Y), 
				(int) MathF.Round(b.X), 
				(int) MathF.Round(b.Y)
			);

		public static Vector3 RotateX(this Vector3 v, float theta) {
			var v2 = v.YZ().Rotate(theta);
			return new(v.X, v2.X, v2.Y);
		}
		
		public static Vector3 RotateY(this Vector3 v, float theta) {
			var v2 = v.XZ().Rotate(theta);
			return new(v2.X, v.Y, v2.Y);
		}
		
		public static Vector3 RotateZ(this Vector3 v, float theta) {
			var v2 = v.XY().Rotate(theta);
			return new(v2, v.Z);
		}
	}
}