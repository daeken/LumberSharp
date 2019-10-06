using System;
using System.Numerics;

namespace MarchingBand {
	public static class ShaderMath {
		public static Vector2 vec2() => Vector2.Zero;
		public static Vector2 vec2(float v) => new Vector2(v, v);
		public static Vector2 vec2(float x, float y) => new Vector2(x, y);
		
		public static Vector3 vec3() => Vector3.Zero;
		public static Vector3 vec3(float v) => new Vector3(v, v, v);
		public static Vector3 vec3(float x, float y, float z) => new Vector3(x, y, z);
		public static Vector3 vec3(Vector2 xy, float z) => new Vector3(xy.X, xy.Y, z);
		public static Vector3 vec3(float x, Vector2 yz) => new Vector3(x, yz.X, yz.Y);
		
		public static Vector4 vec4() => Vector4.Zero;
		public static Vector4 vec4(float v) => new Vector4(v, v, v, v);
		public static Vector4 vec4(float x, float y, float z, float w) => new Vector4(x, y, z, w);
		public static Vector4 vec4(Vector2 xy, float z, float w) => new Vector4(xy.X, xy.Y, z, w);
		public static Vector4 vec4(float x, Vector2 yz, float w) => new Vector4(x, yz.X, yz.Y, w);
		public static Vector4 vec4(float x, float y, Vector2 zw) => new Vector4(x, y, zw.X, zw.Y);
		public static Vector4 vec4(Vector2 xy, Vector2 zw) => new Vector4(xy.X, xy.Y, zw.X, zw.Y);
		public static Vector4 vec4(Vector3 xyz, float w) => new Vector4(xyz.X, xyz.Y, xyz.Z, w);
		public static Vector4 vec4(float x, Vector3 yzw) => new Vector4(x, yzw.X, yzw.Y, yzw.Z);

		public static Vector2 normalize(Vector2 vec) => Vector2.Normalize(vec);
		public static Vector3 normalize(Vector3 vec) => Vector3.Normalize(vec);
		public static Vector4 normalize(Vector4 vec) => Vector4.Normalize(vec);
		
		public static float sin(float value) => MathF.Sin(value);
		public static float cos(float value) => MathF.Cos(value);
		public static float tan(float value) => MathF.Tan(value);
		public static float clamp(float x, float min, float max) => MathF.Max(min, MathF.Min(x, max));
		public static float mix(float x, float y, float a) => x * (1 - a) + y * a;
	}
}