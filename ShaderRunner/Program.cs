using System;
using System.Numerics;
using MarchingBand;
using static MarchingBand.ShaderMath;

namespace ShaderRunner {
	struct Pixel {
		public Vector4 Color;
		public Vector3 Normal;
		public float Depth;
	}

	class Shader : BaseShader<Pixel> {
		public override Pixel Evaluate(Vector2 position) {
			return new Pixel {
				Color = Vector4.One, 
				Normal = normalize(vec3(1, 2, 3)),
				Depth = Testing(5f + sin(6f), 15)
			};
		}

		static float Testing(float a, float b) => a + b;
	}
	
	class Program {
		static void Main(string[] args) {
			var shader = new Shader();
			Console.WriteLine(shader.CompileGlsl());
		}
	}
}