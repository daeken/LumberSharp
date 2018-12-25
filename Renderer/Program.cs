using System;
using System.IO;
using System.Linq;
using System.Numerics;
using ImageLib;
using PrettyPrinter;

namespace Renderer {
	class Program {
		static byte ToColor(float v) {
			if(v > 1) return 255;
			if(v < 0) return 0;
			return (byte) MathF.Round(v * 255);
		}
		
		static void Main(string[] args) {
			"Loading".Print();
			
			var scene = new Scene();
			var eggMesh = StlLoader.Load(File.ReadAllText("egg-2.stl"));
			var cubeMesh = StlLoader.Load(File.ReadAllText("block100.stl"));
			var sphereMesh = StlLoader.Load(File.ReadAllText("sphere.stl"));
			var mirrorMesh = StlLoader.Load(File.ReadAllText("mirror.stl"));
			scene.Add(new Mesh(eggMesh.Translate(new Vector3(0, 30, 0)).ToList()));
			scene.Add(new Mesh(eggMesh.Translate(new Vector3(40, 0, 0)).ToList()));
			//scene.Add(new Mesh(mirrorMesh.Translate(new Vector3(0, 50, 50)).ToList()));
			//scene.Add(new Mesh(cubeMesh.Scale(new Vector3(.5f)).Translate(new Vector3(-60, 40, 0)).ToList(), mat));
			scene.Add(new Mesh(sphereMesh.Translate(new Vector3(-55, 65, 20)).ToList()));
			
			var camera = new PerspectiveCamera { Position = new Vector3(0, -100, 25) }.LookAt(Vector3.Zero);
			var raytracer = new Raytracer(scene, camera);

			var side = 4000;
			"Rendering".Print();
			var pixels = raytracer.Render(side, side);
			"Building images".Print();
			var nimage = new Image(ColorMode.Rgb, (side, side),
				pixels.Select(x => new[] {
					ToColor(x.Normal.X / 2f + .5f), ToColor(x.Normal.Y / 2f + .5f), 
					ToColor(x.Normal.Z / 2f + .5f)
				}).SelectMany(x => x).ToArray()
			);
			var dimage = new Image(ColorMode.Rgba, (side, side),
				pixels.Select(x => BitConverter.GetBytes(x.Depth)).SelectMany(x => x).ToArray()
			);
			"Exporting PNGs".Print();
			using(var fp = File.OpenWrite("test4kn.png"))
				Png.Encode(nimage, fp);
			using(var fp = File.OpenWrite("test4kd.png"))
				Png.Encode(dimage, fp);
		}
	}
}