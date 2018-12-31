using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Common;
using ImageLib;
using IronPython.Hosting;

namespace ParticlePlot {
	class Program {
		static void Main(string[] args) {
			if(args.Length != 2) {
				Console.Error.WriteLine("Usage: dotnet run <script.py> <output.svg>");
				Environment.Exit(1);
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
			
			var paths = system.AllParticles.Select(x => x.PositionHistory.Select(y => y.Item2).ToList()).ToList();
			if(paths.Count == 0) {
				Console.Error.WriteLine("No particles created. No SVG generated.");
				Console.Error.WriteLine("Did you forget to run the particle system?");
				Environment.Exit(1);
			}

			paths = SvgHelper.SimplifyPaths(paths, 2);
			paths = SvgHelper.ReorderPaths(paths);
			SvgHelper.Output(args[1], paths, page);
		}
	}
}