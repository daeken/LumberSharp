using System.Collections.Generic;
using System.Numerics;

namespace Lightness.Renderer {
	public class Model {
		public readonly IReadOnlyList<Triangle> Mesh;
		public Matrix4x4 Transform = Matrix4x4.Identity;

		public Model(IReadOnlyList<Triangle> mesh) => Mesh = mesh;
		public Model(IReadOnlyList<Triangle> mesh, Matrix4x4 transform) {
			Mesh = mesh;
			Transform = transform;
		}

		public Model Translate(Vector3 trans) =>
			new Model(Mesh, Transform * Matrix4x4.CreateTranslation(trans));

		public Model Rotate(Vector3 axis, float angle) =>
			new Model(Mesh, Transform * Matrix4x4.CreateFromAxisAngle(axis, angle));
	}
}