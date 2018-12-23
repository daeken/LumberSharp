using System.Numerics;

namespace Converter {
	public struct Pixel {
		public Vector4 Color;
		public Vector3 Normal;
		public float Depth;
		public bool Edge;
		public float Distance;
	}
}