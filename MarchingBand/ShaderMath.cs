using System;
using System.Numerics;

namespace MarchingBand {
	public static class ShaderMath {
		public static Vec2 vec2() => Vec2.Zero;
		public static Vec2 vec2(float v) => new Vec2(v, v);
		public static Vec2 vec2(float x, float y) => new Vec2(x, y);
		
		public static Vec3 vec3() => Vec3.Zero;
		public static Vec3 vec3(float v) => new Vec3(v, v, v);
		public static Vec3 vec3(float x, float y, float z) => new Vec3(x, y, z);
		public static Vec3 vec3(Vec2 xy, float z) => new Vec3(xy.x, xy.y, z);
		public static Vec3 vec3(float x, Vec2 yz) => new Vec3(x, yz.x, yz.y);
		
		public static Vec4 vec4() => Vec4.Zero;
		public static Vec4 vec4(float v) => new Vec4(v, v, v, v);
		public static Vec4 vec4(float x, float y, float z, float w) => new Vec4(x, y, z, w);
		public static Vec4 vec4(Vec2 xy, float z, float w) => new Vec4(xy.x, xy.y, z, w);
		public static Vec4 vec4(float x, Vec2 yz, float w) => new Vec4(x, yz.x, yz.y, w);
		public static Vec4 vec4(float x, float y, Vec2 zw) => new Vec4(x, y, zw.x, zw.y);
		public static Vec4 vec4(Vec2 xy, Vec2 zw) => new Vec4(xy.x, xy.y, zw.x, zw.y);
		public static Vec4 vec4(Vec3 xyz, float w) => new Vec4(xyz.x, xyz.y, xyz.z, w);
		public static Vec4 vec4(float x, Vec3 yzw) => new Vec4(x, yzw.x, yzw.y, yzw.z);

		public static Vec2 normalize(Vec2 vec) => vec.Normalized;
		public static Vec3 normalize(Vec3 vec) => vec.Normalized;
		public static Vec4 normalize(Vec4 vec) => vec.Normalized;
		
		public static float sin(float value) => MathF.Sin(value);
		public static float cos(float value) => MathF.Cos(value);
		public static float tan(float value) => MathF.Tan(value);
		public static float clamp(float x, float min, float max) => MathF.Max(min, MathF.Min(x, max));
		public static float mix(float x, float y, float a) => x * (1 - a) + y * a;

		public static float length(Vec2 v) => v.Length;
		public static float length(Vec3 v) => v.Length;
		public static float length(Vec4 v) => v.Length;
	}
}