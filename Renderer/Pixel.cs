using System.Numerics;

namespace Renderer {
	public struct Pixel {
		public Vector3 Color;
		public Vector3 Normal, Position;
		public float Depth;
	}
}