using System.Numerics;

namespace Renderer {
	public class Material {
		public float Reflectivity = -1;
		public float Transparency = -1;
		public float RefractiveIndex = -1;

		public Vector3 Albedo;
	}
}