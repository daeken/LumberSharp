using System.Collections.Generic;
using System.Numerics;

namespace Lightness.Renderer {
	public class Model {
		public readonly IReadOnlyList<Triangle> Mesh;
		public Matrix4x4 Transform = Matrix4x4.Identity;

		public Model(IReadOnlyList<Triangle> mesh) => Mesh = mesh;

		public Model Translate(Vector3 trans) {
			Transform *= Matrix4x4.CreateTranslation(trans);
			return this;
		}

		public Model Rotate(Vector3 axis, float angle) {
			Transform *= Matrix4x4.CreateFromAxisAngle(axis, angle);
			return this;
		}
	}
}