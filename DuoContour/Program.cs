using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DoubleSharp.MathPlus;
using Common;

namespace DuoContour {
	class Program {
		const float Epsilon = 0.0001f;
		const float Resolution = 0.01f;
		static readonly float ResolutionSquare = MathF.Sqrt(Resolution*Resolution*2);

		static void Main(string[] args) {
			var bounds = FindBounds(Scene);
			var paths = Enumerable.Range(0, 5).Select(x => x == 0 ? 0 : -MathF.Pow(.1f, (x + 1) / 2f))
				.Select(d => FindIsoLines(p => Scene(p) + d, bounds))
				.SelectMany(x => x);
			
			SvgHelper.Output("test.svg", paths.Select(x => ("black", x)).ToList(), new Page());
		}

		static List<List<Vector2>> FindIsoLines(Func<Vector2, float> f, (Vector2, Vector2) bounds) {
			var points = new HashSet<Vector2>();
			
			var (lb, ub) = bounds;
			var size = ub - lb;
			var (xSteps, ySteps) = (size / Resolution).CeilInt();

			var paths = new List<List<Vector2>>();

			for(var y = 0; y < ySteps; ++y)
				for(var x = 0; x < xSteps; ++x) {
					var p = new Vector2(lb.X + x * Resolution, lb.Y + y * Resolution);
					if(f(p) > ResolutionSquare) continue;
					var path = new List<Vector2>();
					while(true) {
						p = FindClosestSurfacePoint(f, p);
						path.Add(p);
						var rp = p.Apply(v => MathF.Round(v, 2));
						if(points.Contains(rp)) break;
						points.Add(rp);
						var normal = FirstDerivative(f, p);
						p += normal.Rotate(MathF.Tau / 4) * (Resolution / 2);
					}

					if(path.Count > 1)
						paths.Add(path);
				}
			
			Console.WriteLine($"Got {paths.Count} isolines!");

			paths = SvgHelper.ScalePaths(paths, 100);
			paths = SvgHelper.TriviallyJoinPaths(paths);
			paths = SvgHelper.ReorderPaths(paths);
			paths = SvgHelper.JoinPaths(paths);
			paths = SvgHelper.SimplifyPaths(paths, 0.1f);
			paths = SvgHelper.ReorderPaths(paths);
			paths = SvgHelper.JoinPaths(paths);
			paths = SvgHelper.ScalePaths(paths, 1f / 100);
			paths = SvgHelper.ReorderPaths(paths);
			
			Console.WriteLine($"{paths.Count} after cleanup");

			return paths;
		}

		static Vector2 FindClosestSurfacePoint(Func<Vector2, float> f, Vector2 p) {
			while(true) {
				var d = f(p);
				if(MathF.Abs(d) <= Epsilon) return p;
				//Console.WriteLine($"{p} -- {d}");
				p -= FirstDerivative(f, p) * (d / 2);
			}
		}

		static (Vector2 Lower, Vector2 Upper) FindBounds(Func<Vector2, float> sf) {
			const float MinPad = 10f;
			var sp = FindClosestSurfacePoint(sf, new(-1000f));
			float FindBound(Func<float, float> f, float v) {
				while(true) {
					var d = f(v);
					if(d < MinPad) {
						v += MinPad;
						continue;
					}

					var dd = f(v + MinPad);
					if(dd < MinPad) {
						v += MinPad * 2;
						continue;
					}
					if(dd > d) return v;
					v += MinPad;
				}
			}

			var lb = new Vector2(-FindBound(v => sf(new(-v, sp.Y)), sp.X), -FindBound(v => sf(new(sp.X, -v)), sp.Y));
			return (
				lb, 
				new(FindBound(v => sf(new(v, sp.Y)), lb.X), FindBound(v => sf(new(sp.X, v)), lb.Y))
			);
		}

		static Vector2 FirstDerivative(Func<Vector2, float> f, Vector2 p) =>
			Vector2.Normalize(new(
				f(new(p.X + Epsilon, p.Y)) - f(new(p.X - Epsilon, p.Y)),
				f(new(p.X, p.Y + Epsilon)) - f(new(p.X, p.Y - Epsilon)) 
			));

		static float Scene(Vector2 p) =>
			Union(
				Circle(p, 1), 
				Box(p + Vector2.One, new(.5f))
			);

		static float Union(params float[] objs) => objs.Min();
		static float Intersect(params float[] objs) => objs.Max();
		static float Circle(Vector2 p, float r) => p.Length() - r;

		static float Box(Vector2 p, Vector2 b) {
			var d = p.Abs() - b;
			return (d.Max(Vector2.Zero) + new Vector2(MathF.Min(MathF.Max(d.X, d.Y), 0))).Length();
		}
	}
}