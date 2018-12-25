using System;
using System.IO;
using System.Linq;
using System.Numerics;
using ImageLib;
using Lightness.Renderer;
using PrettyPrinter;

namespace Lightness {
	class Program {
		static void Main(string[] args) {
			"Loading".Print();
			var scene = new Scene();
			//var egg2Mesh = StlLoader.Load("egg-2.stl");
			//scene.Add(new Model(egg2Mesh).Rotate(Vector3.UnitZ, MathF.PI / 5).Translate(new Vector3(0, 30, -25)));
			//var radioTowerMesh = StlLoader.Load("radiotower.stl");
			//scene.Add(new Model(radioTowerMesh).Rotate(Vector3.UnitZ, MathF.PI / 4).Translate(new Vector3(0, 250, -100)));
			var statueMesh = StlLoader.Load("statue.stl");
			scene.Add(new Model(statueMesh).Translate(new Vector3(-225, -225, -60)));
			
			var camera = new PerspectiveCamera {
				Up = Vector3.UnitZ, 
				Position = new Vector3(0, -100, 0), 
				LookAt = Vector3.Zero, 
				FOV = 45
			};

			const int width = 4000;
			const int height = 4000;
			
			"Rendering".Print();
			var renderer = new Renderer.Renderer(scene, camera, (width, height));
			renderer.Rendered += pixels => {
				/*"Outputting image".Print();
				var nimage = new Image(ColorMode.Rgb, (width, height),
					pixels.Select(x => x == null ? new byte[] { 0, 0, 0 } : new[] {
						ToColor(x.Normal.X / 2f + .5f), ToColor(x.Normal.Y / 2f + .5f), 
						ToColor(x.Normal.Z / 2f + .5f)
					}).SelectMany(x => x).ToArray()
				);
				using(var fp = File.OpenWrite("test4kn.png"))
					Png.Encode(nimage, fp);
				return;*/
				
				"Vectorizing".Print();
				var vectorize = new Vectorize(pixels, width, height);
				vectorize.Output("test.svg");
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