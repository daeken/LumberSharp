using System;
using System.Numerics;

namespace Renderer {
	public abstract class Camera {
		public Vector3 Position, Direction, Up = Vector3.UnitZ;

		public Camera LookAt(Vector3 at) {
			Direction = (at - Position).Normalized();
			return this;
		}
		
		public abstract Ray GenerateRay(float x, float y, float aspectRatio);
	}

	public class PerspectiveCamera : Camera {
		const float FOV = 45;

		public override Ray GenerateRay(float x, float y, float aspectRatio) {
			var angle = MathF.Tan(MathF.PI * 0.5f * FOV / 180f);
			var dir = new Vector3(x * angle * aspectRatio, 1, y * angle);
			return new Ray { Origin = Position, Direction = dir };
		}
	}
}