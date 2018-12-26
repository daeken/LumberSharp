using System.Numerics;

namespace Lightness.Renderer {
	public class Pixel {
		public readonly Vector3 Normal;
		public readonly float Depth;

		public float DepthDelta, AngleDelta; // To surrounding pixels
		public bool Edge, Flooded;

		public Pixel(Vector3 normal, float depth) {
			Normal = normal;
			Depth = depth;
		}
	}
}