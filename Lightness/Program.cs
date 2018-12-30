using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Common;
using ImageLib;
using Lightness.Renderer;
using MoonSharp.Interpreter;
using PrettyPrinter;

namespace Lightness {
	class Program {
		public static string BaseDirectory;
	
		static void Main(string[] args) {
			if(args.Length != 2) {
				Console.Error.WriteLine("Usage: dotnet run <script.lua> <output.svg>");
				Environment.Exit(1);
			}

			BaseDirectory = Path.GetDirectoryName(Path.GetFullPath(args[0]));
			
			"Loading".Debug();
			var scene = new Scene();
			var page = new Page();
			
			UserData.RegisterAssembly(typeof(Program).Assembly);
			UserData.RegisterType<Vector3>();
			UserData.RegisterType<Page>();
			var script = new Script();
			script.Globals["vec3"] = (Func<float, float, float, Vector3>) ((a, b, c) => new Vector3(a, b, c));
			script.Globals["scene"] = scene;
			script.Globals["page"] = page;
			script.Globals["StlLoader"] = typeof(StlLoader);
			script.Globals["PerspectiveCamera"] = typeof(PerspectiveCamera);
			script.Globals["PI"] = MathF.PI;
			script.DoStream(File.OpenRead(args[0]));

			if(scene.Camera == null) {
				Console.Error.WriteLine("ERROR: Camera not assigned to scene.");
				Environment.Exit(1);
			}
			
			"Rendering".Debug();
			var renderer = new Renderer.Renderer(scene, (scene.Width, scene.Height));
			renderer.Rendered += pixels => {
				if(scene.Preview || scene.EdgePreview) {
					if(scene.EdgePreview)
						new Vectorize(pixels, scene.Width, scene.Height, true);
					"Outputting image".Debug();
					var nimage = scene.EdgePreview
						? new Image(ColorMode.Greyscale, (scene.Width, scene.Height),
							pixels.Select(x => new[] { x == null || !x.Edge ? (byte) 0 : (byte) 255 })
								.SelectMany(x => x).ToArray())
						: new Image(ColorMode.Rgb, (scene.Width, scene.Height),
							pixels.Select(x => x == null
								? new byte[] { 0, 0, 0 }
								: new[] {
									ToColor(x.Normal.X / 2f + .5f), ToColor(x.Normal.Y / 2f + .5f),
									ToColor(x.Normal.Z / 2f + .5f)
								}).SelectMany(x => x).ToArray());
					using(var fp = File.OpenWrite("preview.png"))
						Png.Encode(nimage, fp);
				} else {
					"Vectorizing".Debug();
					var vectorize = new Vectorize(pixels, scene.Width, scene.Height, false);
					vectorize.Output(args[1], page);
				}
			};
			renderer.Render();
		}
		
		static byte ToColor(float v) {
			if(v > 1) return 255;
			if(v < 0) return 0;
			return (byte) MathF.Round(v * 255);
		}
	}
}