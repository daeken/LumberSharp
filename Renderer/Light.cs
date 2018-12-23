using System.Numerics;

namespace Renderer {
	public abstract class Light {
		public Vector3 Color;
	}

	public class DirectionalLight : Light {
		public Vector3 Direction;
	}
}