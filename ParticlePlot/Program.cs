using System;
using System.Linq;
using System.Numerics;
using Common;
using ImageLib;
using IronPython.Hosting;

namespace ParticlePlot {
	class Program {
		static int Main(string[] args) {
			if(args.Length != 2) {
				Console.Error.WriteLine("Usage: dotnet run <script.py> <output.svg>");
				return 1;
			}

			var engine = Python.CreateEngine();
			var scope = engine.CreateScope();
			var system = new ParticleSystem();
			var source = engine.CreateScriptSourceFromFile(args[0]);

			var page = new Page();
			
			scope.SetVariable("page", page);
			scope.SetVariable("vec2", (Func<float, float, Vector2>) ((a, b) => new Vector2(a, b)));
			scope.SetVariable("rotate", (Func<Vector2, float, Vector2>) Extensions.Rotate);
			scope.SetVariable("particleSystem", system);
			scope.SetVariable("Attractor", typeof(Attractor));
			scope.SetVariable("GlobalAccelerator", typeof(GlobalAccelerator));
			scope.SetVariable("RadialGenerator", typeof(RadialGenerator));
			scope.SetVariable("Repeller", typeof(Repeller));
			scope.SetVariable("Image", typeof(Image));
			scope.SetVariable("LoadPng", (Func<string, Image>) Png.Decode);
			source.Execute(scope);
			
			var paths = system.AllParticles
				.Select(particle => particle.PositionHistory.Select(sample => sample.Position).ToList())
				.ToList();
			if(paths.Count == 0) {
				Console.Error.WriteLine("No particles created. No SVG generated.");
				Console.Error.WriteLine("Did you forget to run the particle system?");
				return 1;
			}

			paths = SvgHelper.SimplifyPaths(paths, 5);
			paths = SvgHelper.ReorderPaths(paths);
			SvgHelper.Output(args[1], paths.Select(x => ("black", x)).ToList(), page, true);
			return 0;
		}
	}
}
