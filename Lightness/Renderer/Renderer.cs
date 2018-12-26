using System;
using System.Linq;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using PrettyPrinter;
using Vector3 = System.Numerics.Vector3;

namespace Lightness.Renderer {
	public class Renderer : GameWindow {
		readonly Scene Scene;
		public new readonly (int Width, int Height) Size;
		
		public event Action<Pixel[]> Rendered;
		
		public Renderer(Scene scene, (int Width, int Height) size) : base(
			100, 100, GraphicsMode.Default, "Lightness",
			GameWindowFlags.Default, DisplayDevice.Default, 4, 1, GraphicsContextFlags.ForwardCompatible
		) {
			Scene = scene;
			Size = size;
		}

		protected override void OnLoad(EventArgs e) {
			WindowState = WindowState.Minimized;
			
			var fbo = new FrameBuffer(Size.Width, Size.Height, FrameBufferAttachment.Xyz, FrameBufferAttachment.Depth);
			fbo.Bind();

			Scene.Camera.AspectRatio = Size.Width / (float) Size.Height;

			var projectionViewMat = Scene.Camera.Matrix;
			
			var program = new Program(@"
#version 410
precision highp float;
layout (location = 0) in vec4 aPosition;
layout (location = 1) in vec3 aNormal;
uniform mat4 uModelMat;
uniform mat4 uProjectionViewMat;
out vec3 vNormal;
void main() {
	gl_Position = uProjectionViewMat * uModelMat * aPosition * vec4(1, -1, 1, 1);
	mat3 nmat = transpose(inverse(mat3(uModelMat)));
	vNormal = normalize(nmat * aNormal);
}

					", @"
#version 410
precision highp float;
in vec3 vNormal;
out vec3 normal;
void main() {
	normal = vNormal;
}
					");
			program.Use();
			program.SetUniform("uProjectionViewMat", projectionViewMat);
			
			GL.Viewport(0, 0, Size.Width, Size.Height);
			GL.ClearColor(0, 0, 0, 0);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
		
			GL.Disable(EnableCap.CullFace);
			GL.Disable(EnableCap.Blend);
			GL.Enable(EnableCap.DepthTest);

			foreach(var model in Scene.Models) {
				program.SetUniform("uModelMat", model.Transform);
				var vao = new Vao();
				var buffer = new Buffer<Vector3>(model.Mesh.Select(x => new[] { x.A, x.NA, x.B, x.NB, x.C, x.NC }).SelectMany(x => x).ToArray());
				vao.Attach(buffer, (0, typeof(Vector3)), (1, typeof(Vector3)));
				vao.Bind(() => GL.DrawArrays(PrimitiveType.Triangles, 0, model.Mesh.Count * 6 * 3));
				vao.Destroy();
				buffer.Destroy();
			}
			
			GL.Finish();

			var normals = fbo.ReadAttachment(FrameBufferAttachment.Xyz);
			var depths = fbo.ReadAttachment(FrameBufferAttachment.Depth);
			
			Close();

			var pixels = new Pixel[Size.Width * Size.Height];
			for(var i = 0; i < pixels.Length; ++i) {
				var normal = new Vector3(normals[i * 3 + 0], normals[i * 3 + 1], normals[i * 3 + 2]);
				if(normal.X == 0 && normal.Y == 0 && normal.Z == 0) continue;
				pixels[i] = new Pixel(normal, depths[i]);
			}
			
			Rendered?.Invoke(pixels);
		}

		public void Render() => Run();
	}
}