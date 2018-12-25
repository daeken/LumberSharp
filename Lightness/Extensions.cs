using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Lightness.Renderer;
using static System.MathF;

namespace Lightness {
	public static class Extensions {
		public static (Vector3 Low, Vector3 High) Bounds(this IEnumerable<Vector3> e) {
			var low = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
			var high = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

			foreach(var v in e) {
				low.X = Min(low.X, v.X);
				low.Y = Min(low.Y, v.Y);
				low.Z = Min(low.Z, v.Z);
				
				high.X = Max(high.X, v.X);
				high.Y = Max(high.Y, v.Y);
				high.Z = Max(high.Z, v.Z);
			}

			return (low, high);
		}

		public static IEnumerable<Triangle> Translate(this IEnumerable<Triangle> tris, Vector3 trans) =>
			tris.Select(x => new Triangle(x.A + trans, x.B + trans, x.C + trans, x.NA, x.NB, x.NC));
		public static IEnumerable<Triangle> Scale(this IEnumerable<Triangle> tris, Vector3 scale) =>
			tris.Select(x => new Triangle(x.A * scale, x.B * scale, x.C * scale, x.NA, x.NB, x.NC));
		
		public static Vector2 XY(this Vector3 v) => new Vector2(v.X, v.Y);
		public static Vector2 XY(this Vector4 v) => new Vector2(v.X, v.Y);
		
		public static Vector3 XZY(this Vector3 v) => new Vector3(v.X, v.Z, v.Y);
		public static Vector3 YXZ(this Vector3 v) => new Vector3(v.Y, v.X, v.Z);
		public static Vector3 ZYX(this Vector3 v) => new Vector3(v.Z, v.Y, v.X);
		
		public static Vector2 Add(this Vector2 v, float right) => new Vector2(v.X + right, v.Y + right);

		public static Vector3 Cross(this Vector3 left, Vector3 right) => Vector3.Cross(left, right);
		public static Vector3 Normalized(this Vector3 v) => Vector3.Normalize(v);
		
		public static float min(float a, float b) => Min(a, b);
		public static Vector2 min(Vector2 a, Vector2 b) => vec2(Min(a.X, b.X), Min(a.Y, b.Y));
		public static Vector3 min(Vector3 a, Vector3 b) => vec3(Min(a.X, b.X), Min(a.Y, b.Y), Min(a.Z, b.Z));
		public static float max(float a, float b) => Max(a, b);
		public static Vector2 max(Vector2 a, Vector2 b) => vec2(Max(a.X, b.X), Max(a.Y, b.Y));
		public static Vector3 max(Vector3 a, Vector3 b) => vec3(Max(a.X, b.X), Max(a.Y, b.Y), Max(a.Z, b.Z));
		
		public static Vector2 vec2() => new Vector2();
		public static Vector2 vec2(float v) => new Vector2(v);
		public static Vector2 vec2(float x, float y) => new Vector2(x, y);

		public static Vector3 vec3() => new Vector3();
		public static Vector3 vec3(float v) => new Vector3(v);
		public static Vector3 vec3(float x, float y, float z) => new Vector3(x, y, z);

		public static Vector4 vec4() => new Vector4();
		public static Vector4 vec4(float v) => new Vector4(v);
		public static Vector4 vec4(Vector3 xyz, float w) => new Vector4(xyz.X, xyz.Y, xyz.Z, w);
		public static Vector4 vec4(float x, float y, float z, float w) => new Vector4(x, y, z, w);

		public static float clamp(float x, float min, float max) => Min(Max(x, min), max);
		public static float fract(float x) => x - Floor(x);

		public static Vector3 floor(Vector3 x) => vec3(Floor(x.X), Floor(x.Y), Floor(x.Z));
		
		public static Vector3 Select(this (Vector3 A, Vector3 B) v, int sa, int sb, int sc) {
			if(sa != 1 && sb != 1 && sc != 1) return v.A;
			if(sa == 1 && sb == 1 && sc == 1) return v.B;
			return new Vector3(
				sa == 1 ? v.B.X : v.A.X, 
				sb == 1 ? v.B.Y : v.A.Y, 
				sc == 1 ? v.B.Z : v.A.Z
			);
		}

		public static float ComponentMin(this Vector3 v) => Min(Min(v.X, v.Y), v.Z);
		public static float ComponentMax(this Vector3 v) => Max(Max(v.X, v.Y), v.Z);
	}
}