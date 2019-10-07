using System.Numerics;
using MarchingBand;
using static MarchingBand.ShaderMath;

namespace Lightness {
	public struct OutputPixel {
		public Vec4 NormalDepth;
	}

	public abstract class BaseMarchShader : BaseShader<OutputPixel> {
		public Vec3 uCameraPosition;
		public Matrix4x4 uCameraMatrix;
		
		public override OutputPixel Evaluate(Vec2 position) {
			var rayDirection = (uCameraMatrix * vec4(normalize(vec3(position, 2)), 0)).xyz;

			var t = CastRay(uCameraPosition, rayDirection);
			return t != 100000
				? new OutputPixel { NormalDepth = vec4(CalcNormal(uCameraPosition + t * rayDirection), t) }
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
		
		public float Torus(Vec3 p, Vec2 t) =>
			length(vec2(length(p.xz) - t.x, p.y)) - t.y;
		
		public Vec3 OpTwist(Vec3 p) {
			var c = cos(10 * p.y + 10);
			var s = sin(10 * p.y + 10);
			return vec3(p.x * c - p.z * s, p.x * s + p.z * c, p.y);
		}

		protected abstract float Map(Vec3 pos);
	}
}