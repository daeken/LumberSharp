using System;
using System.Linq;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using PrettyPrinter;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

namespace Lightness.Renderer {
	public class Renderer : GameWindow {
		readonly Scene Scene;
		public new readonly (int Width, int Height) Size;
		
		public event Action<Pixel[]> Rendered;
		
		public Renderer(Scene scene, (int Width, int Height) size) : base(GameWindowSettings.Default, new() {
			ClientSize = new(200, 200),
			Title = "Lightness",
			Flags = ContextFlags.ForwardCompatible,
			APIVersion = Version.Parse("4.1")
		}) {
			Scene = scene;
			Size = size;
		}

		protected override void OnLoad() {
			WindowState = WindowState.Minimized;
			
			var fbo = new FrameBuffer(Size.Width, Size.Height, FrameBufferAttachment.Xyzw, FrameBufferAttachment.Depth);
			fbo.Bind();

			Scene.Camera.AspectRatio = Size.Width / (float) Size.Height;

			GL.Viewport(0, 0, Size.Width, Size.Height);
			GL.ClearColor(0, 0, 0, 0);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
		
			GL.Disable(EnableCap.CullFace);
			GL.Disable(EnableCap.Blend);
			GL.Enable(EnableCap.DepthTest);

			if(Scene is RasterScene rscene) {
				var projectionViewMat = Scene.Camera.Matrix;
				
				var program = new Program(@"
#version 410
precision highp float;
layout (location = 0) in vec4 aPosition;
layout (location = 1) in vec3 aNormal;
uniform mat4 uModelMat;
uniform mat4 uProjectionViewMat;
out vec3 vNormal;
out vec3 vPosition;
void main() {
	gl_Position = uProjectionViewMat * uModelMat * aPosition * vec4(1, -1, 1, 1);
	mat3 nmat = transpose(inverse(mat3(uModelMat)));
	vNormal = normalize(nmat * aNormal);
	vPosition = aPosition.xyz;
}

					", @"
#version 410
precision highp float;
uniform vec3 uCameraPosition;
in vec3 vNormal;
in vec3 vPosition;
out vec4 normalDepth;
void main() {
	normalDepth = vec4(vNormal, length(uCameraPosition - vPosition));
}
					");
				program.Use();
				program.SetUniform("uProjectionViewMat", projectionViewMat);
				program.SetUniform("uCameraPosition", Scene.Camera.Position);

				foreach(var model in rscene.Models) {
					program.SetUniform("uModelMat", model.Transform);
					var vao = new Vao();
					var buffer = new Buffer<Vector3>(model.Mesh.Select(x => new[] { x.A, x.NA, x.B, x.NB, x.C, x.NC })
						.SelectMany(x => x).ToArray());
					vao.Attach(buffer, (0, typeof(Vector3)), (1, typeof(Vector3)));
					vao.Bind(() => GL.DrawArrays(PrimitiveType.Triangles, 0, model.Mesh.Count * 6 * 3));
					vao.Destroy();
					buffer.Destroy();
				}
			} else if(Scene is RaymarchedScene mscene) {
				var program = new Program(@"
#version 410
precision highp float;
layout (location = 0) in vec3 aPosition;
out vec2 vPosition;
void main() {
	gl_Position = vec4(aPosition, 1);
	vPosition = aPosition.xy * vec2(1, -1);
}
					", mscene.FragmentShader);
				program.Use();
				program.SetUniform("uCameraPosition", Scene.Camera.Position);
				program.SetUniform("uCameraUp", Scene.Camera.Up);
				program.SetUniform("uLookAt", Scene.Camera.LookAt);
				program.SetUniform("uCameraMatrix", Scene.Camera.ViewMatrix);
				GL.DepthFunc(DepthFunction.Always);
				var vao = new Vao();
				var buffer = new Buffer<Vector2>(new[] {
					new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), 
					new Vector2(1, -1), new Vector2(1, 1), new Vector2(-1, 1)
				});
				vao.Attach(buffer, (0, typeof(Vector2)));
				vao.Bind(() => GL.DrawArrays(PrimitiveType.Triangles, 0, 6));
				vao.Destroy();
				buffer.Destroy();
			}

			GL.Finish();

			var normalsDepths = fbo.ReadAttachment(FrameBufferAttachment.Xyzw);
			
			Close();

			var pixels = new Pixel[Size.Width * Size.Height];
			for(var i = 0; i < pixels.Length; ++i) {
				var normal = new Vector3(normalsDepths[i * 4 + 0], normalsDepths[i * 4 + 1], normalsDepths[i * 4 + 2]);
				if(normal.X == 0 && normal.Y == 0 && normal.Z == 0) continue;
				pixels[i] = new Pixel(normal, normalsDepths[i * 4 + 3]);
			}
			
			Rendered?.Invoke(pixels);
		}

		public void Render() => Run();
	}
}