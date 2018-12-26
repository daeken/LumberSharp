using System;
using System.Numerics;
using MoonSharp.Interpreter;

namespace Lightness.Renderer {
	[MoonSharpUserData]
	public abstract class Camera {
		public Vector3 Up = Vector3.UnitZ;
		public Vector3 Position = Vector3.Zero;
		public Vector3 LookAt = Vector3.UnitX;

		public float AspectRatio;
		
		protected abstract Matrix4x4 ProjectionMatrix { get; }
		
		public Matrix4x4 Matrix => Matrix4x4.CreateLookAt(Position, LookAt, Up) * ProjectionMatrix;
	}

	[MoonSharpUserData]
	public class PerspectiveCamera : Camera {
		public float FOV = 45;

		protected override Matrix4x4 ProjectionMatrix => Matrix4x4.CreatePerspectiveFieldOfView(FOV * (MathF.PI / 180), AspectRatio, 1, 5000);
		
		public static PerspectiveCamera Create() => new PerspectiveCamera();
	}
}