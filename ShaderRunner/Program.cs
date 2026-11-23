using System;
using System.Numerics;
using MarchingBand;
using static MarchingBand.ShaderMath;

namespace ShaderRunner {
	struct Pixel {
		public Vec4 NormalDepth;
	}

	class Shader : BaseShader<Pixel> {
		public Vec3 uCameraPosition = Vec3.Zero;
		public Matrix4x4 uCameraMatrix = Matrix4x4.Identity;
		
		public override Pixel Evaluate(Vec2 position) {
			var rayDirection = (uCameraMatrix * vec4(normalize(vec3(position, 2)), 0)).xyz;

			var t = CastRay(uCameraPosition, rayDirection);
			return t != 100000
				? new Pixel {
					NormalDepth = vec4(CalcNormal(uCameraPosition + t * rayDirection), t)
				}
				: default;
		}

		float CastRay(Vec3 ro, Vec3 rd) {
			var t = 1f;
			const float tmax = 20;

			for(var i = 0; i < 64; ++i) {
				var precis = 0.0004f * t;
				var res = Map(ro + rd * t);
				if(res < precis || t > tmax) break;
				t += res;
			}
			
			return t <= tmax ? t : 100000;
		}

		Vec3 CalcNormal(Vec3 pos) {
			var e = vec2(1, -1) * 0.5773f * 0.0005f;
			return normalize(e.xyy * Map(pos + e.xyy) + 
			                 e.yyx * Map(pos + e.yyx) + 
			                 e.yxy * Map(pos + e.yxy) + 
			                 e.xxx * Map(pos + e.xxx));
		}
		
		float Torus(Vec3 p, Vec2 t) =>
			length(vec2(length(p.xz) - t.x, p.y)) - t.y;
		
		Vec3 OpTwist(Vec3 p) {
			var c = cos(10 * p.y + 10);
			var s = sin(10 * p.y + 10);
			return vec3(p.x * c - p.z * s, p.x * s + p.z * c, p.y);
		}

		float Map(Vec3 pos) => 0.5f * Torus(OpTwist(pos - vec3(-2, 0.25f, 2)), vec2(0.2f, 0.05f));
	}
	
	class Program {
		static void Main() => Console.WriteLine(new Shader().CompileGlsl());
	}
}
