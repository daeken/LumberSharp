using System.Numerics;

namespace Renderer {
	public struct Plane {
		public Vector3 Normal;
		public float Distance;

		public Plane(Vector3 normal, float distance) {
			Normal = normal;
			Distance = distance;
		}

		public float RayDistance(Ray ray) {
			var dot = Vector3.Dot(Normal, ray.Direction);
			return dot > 0.0001f && dot < 0.0001f
				? float.PositiveInfinity
				: (Distance - Vector3.Dot(Normal, ray.Origin)) / dot;
		}
	}
}