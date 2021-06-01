using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Common;
using static Common.Helpers;
using static System.MathF;

namespace DuoContour {
	class Program {
		const float Epsilon = 0.0001f;
		const float GridDistance = 0.1f;
		const float GridEpsilon = GridDistance / 100;
		const int ScreenResolution = 1024;
		const float ResolutionScale = 100f / ScreenResolution;
		
		static Vector3 CameraPos = new(2.5f, -2.5f, 5f);
		static Vector3 LookAt = Vector3.Zero;
		const float FocalLength = 2.5f;
		static Matrix4x4 CameraMatrix = CalcCamera(CameraPos, LookAt, 0);

		static void Main(string[] args) {
			var axes = new Func<Vector3, float>[] {
				p => p.X, 
				p => p.Y, 
				p => p.Z
			};
			var paths = axes.Select(axis => FindIsoLines(Scene, axis)).SelectMany(x => x).ToList();

			paths = SvgHelper.Fit(paths, new(1000));
			Console.WriteLine($"Got {paths.Count} total isolines!");

			/*Console.WriteLine("SimplifyPaths");
			paths = SvgHelper.SimplifyPaths(paths, 0.01f);
			Console.WriteLine("TriviallyJoinPaths");
			paths = SvgHelper.TriviallyJoinPaths(paths);
			Console.WriteLine("JoinPaths");
			//paths = SvgHelper.JoinPaths(paths, .01f);
			Console.WriteLine("ReorderPaths");
			paths = SvgHelper.ReorderPaths(paths);
			Console.WriteLine("SimplifyPaths");
			paths = SvgHelper.SimplifyPaths(paths, 0.025f);
			Console.WriteLine("ReorderPaths");
			paths = SvgHelper.ReorderPaths(paths);*/

			Console.WriteLine($"{paths.Count} after cleanup");
			Console.WriteLine($"{SvgHelper.CalcDrawDistance(paths)} total units drawn");

			SvgHelper.Output("test.svg", paths.Select(x => ("black", x)).ToList(), new Page());
		}

		static Matrix4x4 CalcCamera(Vector3 ro, Vector3 ta, float cr) {
			var cw = Vector3.Normalize(ta - ro);
			var cp = new Vector3(Sin(cr), Cos(cr), 0);
			var cu = Vector3.Normalize(Vector3.Cross(cw, cp));
			var cv = Vector3.Cross(cu, cw);
			return new Matrix4x4(
				cu.X, cu.Y, cu.Z, 0,
				cv.X, cv.Y, cv.Z, 0, 
				cw.X, cw.Y, cw.Z, 0, 
				   0,    0,    0, 1 
			);
		}

		static bool March(Func<Vector3, float> f, Vector3 ro, Vector3 rd, out float t, out Vector3 p, int steps = 250) {
			t = 0f;
			for(var i = 0; i < steps && t <= 20; ++i) {
				p = ro + rd * t;
				var d = f(p);
				if(Abs(d) <= Epsilon * t) return true;

				t += d * 0.95f;
			}
			p = Vector3.Zero;
			return false;
		}

		static List<List<Vector2>> FindIsoLines(Func<Vector3, float> f, Func<Vector3, float> d) {
			var points = new ConcurrentDictionary<(int G, int X, int Y), byte>();

			Vector3 CalcRay(int x, int y) =>
				Vector3.Transform(
					Vector3.Normalize(new(x / (float) ScreenResolution, y / (float) ScreenResolution, FocalLength)),
					CameraMatrix);

			Parallel.For(-ScreenResolution, ScreenResolution + 1, y => {
				for(var x = -ScreenResolution; x <= ScreenResolution; ++x) {
					var rd = CalcRay(x, y);
					if(!March(f, CameraPos, rd, out var t, out var p)) continue;
					p = FindClosestSurfacePoint(f, p);
					var normal = FirstDerivative(f, p);
					var ad = 1 / ((CalcRay(x + 1, y) * t + CameraPos - p).Length() * ScreenResolution);
					var gd = d(p);
					if(GridDistance - Abs((gd + 100000 * GridDistance) % GridDistance) > GridEpsilon) continue;
					points[((int) Round(gd / GridDistance), x, y)] = 0;
				}
			});

			IEnumerable<(int G, int X, int Y)> Neighbors((int, int, int) point) {
				var (g, x, y) = point;
				var nbrs = Enumerable.Range(-1, 3).Select(dx =>
						Enumerable.Range(-1, 3).Where(dy => dx != 0 || dy != 0).Select(dy => (g, x + dx, y + dy)))
					.SelectMany(v => v).Where(points.ContainsKey).ToList();
				if(nbrs.Count != 0) return nbrs.Take(1);
				nbrs = Enumerable.Range(-2, 5).Select(dx =>
						Enumerable.Range(-2, 5).Where(dy => dx != 0 || dy != 0).Select(dy => (g, x + dx, y + dy)))
					.SelectMany(v => v).Where(points.ContainsKey).ToList();
				if(nbrs.Count != 0) return nbrs.Take(1);
				return Enumerable.Range(-3, 7).Select(dx =>
						Enumerable.Range(-3, 7).Where(dy => dx != 0 || dy != 0).Select(dy => (g, x + dx, y + dy)))
					.SelectMany(v => v).Where(points.ContainsKey).Take(1);
			}

			var used = new HashSet<((int, int, int), (int, int, int))>();

			var gpaths = points.Keys.Select(point => Neighbors(point).Where(neighbor => !used.Contains((neighbor, point)))
			.Select(neighbor => {
				used.Add((point, neighbor));
				return (point.G, new List<Vector2> { new(point.X, point.Y), new(neighbor.X, neighbor.Y) });
			})).SelectMany(x => x).GroupBy(x => x.G).ToList();

			var paths = gpaths.AsParallel().Select(x => {
				var paths = x.Select(y => y.Item2).ToList();
				Console.WriteLine($"Got {paths.Count} isolines!");
				if(paths.Count == 0) return paths;
				var sc = paths.Count;

				Console.WriteLine($"Quadtree {sc}");
				paths = new QuadTree(paths).GetPaths();
				Console.WriteLine($"JoinPaths {sc}");
				paths = SvgHelper.JoinPaths(paths, 25);
			
				Console.WriteLine($"ReorderPaths {sc}");
				paths = SvgHelper.ReorderPaths(paths);
			
				Console.WriteLine($"{paths.Count} / {sc} after cleanup");

				return paths;
			}).SelectMany(x => x).ToList();
			
			paths = SvgHelper.ScalePaths(paths, ResolutionScale);
			paths = SvgHelper.SimplifyPaths(paths, 1f);
			return paths;
		}

		static Vector3 FindClosestSurfacePoint(Func<Vector3, float> f, Vector3 p) {
			for(var i = 0; i < 10000; ++i) {
				var d = f(p);
				if(d >= 0 && d <= Epsilon) return p;
				if(d < 0)
					p -= FirstDerivative(f, p) * (d * 1.5f);
				else
					p -= FirstDerivative(f, p) * (d / 2);
			}
			return p;
		}

		static Vector3 FirstDerivative(Func<Vector3, float> f, Vector3 p) =>
			Vector3.Normalize(Gradient(f, p));

		static Vector3 Gradient(Func<Vector3, float> f, Vector3 p) =>
			new(
				f(new(p.X + Epsilon, p.Y, p.Z)) - f(new(p.X - Epsilon, p.Y, p.Z)),
				f(new(p.X, p.Y + Epsilon, p.Z)) - f(new(p.X, p.Y - Epsilon, p.Z)), 
				f(new(p.X, p.Y, p.Z + Epsilon)) - f(new(p.X, p.Y, p.Z - Epsilon))
			);
		
		static Vector3 FirstDerivativeAlong(Func<Vector3, float> f, Vector3 p, (Vector3 X, Vector3 Y, Vector3 Z) u) =>
			Vector3.Normalize(GradientAlong(f, p, u));

		static Vector3 GradientAlong(Func<Vector3, float> f, Vector3 p, (Vector3 X, Vector3 Y, Vector3 Z) u) =>
			(f(p + Epsilon * u.X) - f(p - Epsilon * u.X)) * u.X + 
			(f(p + Epsilon * u.Y) - f(p - Epsilon * u.Y)) * u.Y +
			(f(p + Epsilon * u.Z) - f(p - Epsilon * u.Z)) * u.Z;

		static float Scene(Vector3 p) => Intersect(Box(p, Vector3.One * 1.03f), Sphere(p, 1.3f));

		static float Union(params float[] objs) => objs.Min();
		static float Intersect(params float[] objs) => objs.Max();

		static float Plane(Vector3 p, Vector3 n, float h) => Vector3.Dot(p, n) + h;
		
		static float Sphere(Vector3 p, float r) => p.Length() - r;

		static float Box(Vector3 p, Vector3 b) {
			var q = p.Apply(Abs) - b;
			return Vector3.Max(q, Vector3.Zero).Length() + Min(q.Apply(Max), 0);
		}
	}
}