using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Common;
using static Common.Helpers;
using static System.MathF;

namespace DuoContour {
	class Program {
		const float Epsilon = 0.0001f;
		const float Resolution = 0.01f;
		static readonly float ResolutionCube = Pow(Resolution*Resolution*3, 1f/3);
		
		static Matrix4x4 Projection = Matrix4x4.CreatePerspectiveFieldOfView(Tau / 4, 1, 1f, 1000f);
		static Vector3 CameraPos = new(5f, 2.5f, -5f);
		static Matrix4x4 View = Matrix4x4.CreateLookAt(CameraPos, new Vector3(0.0001f, 0.0001f, 1f), Vector3.UnitY);
		static Matrix4x4 ViewProject = View * Projection;

		static void Main(string[] args) {
			var bounds = (new Vector3(-5), new Vector3(5)); // FindBounds(Scene);
			
			Console.WriteLine(bounds);

			var axes = new[] {
				Vector3.UnitX, 
				Vector3.UnitY, 
				Vector3.UnitZ
			};
			var paths = axes.Select(axis => Enumerable.Range(-12, 25).AsParallel().Select(x => 0.1f * x)
				.Select(d => FindIsoLines(Scene, bounds, axis, d))
				.SelectMany(x => x)).SelectMany(x => x).ToList();

			paths = SvgHelper.Fit(paths, new(1000));
			Console.WriteLine($"Got {paths.Count} total isolines!");

			Console.WriteLine("SimplifyPaths");
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
			paths = SvgHelper.ReorderPaths(paths);

			Console.WriteLine($"{paths.Count} after cleanup");
			Console.WriteLine($"{SvgHelper.CalcDrawDistance(paths)} total units drawn");

			SvgHelper.Output("test.svg", paths.Select(x => ("black", x)).ToList(), new Page());
		}

		static Vector2 Project(Vector3 point) {
			var p4 = Vector4.Transform(point, ViewProject);
			return new Vector2(p4.X, p4.Y) * p4.W;
		}

		static float March(Func<Vector3, float> f, Vector3 from, Vector3 to, int steps = 256) {
			var rd = Vector3.Normalize(to - from);
			var t = 0f;
			for(var i = 0; i < steps; ++i) {
				var d = f(from + rd * t);
				if(Abs(d) <= Epsilon)
					return (to - from).Length() - t;
				t += d * 0.9f;
			}
			return 0;
		}

		static Vector3 Realign(Vector3 p, Vector3 dir) {
			var root = Project(p);
			float Error(Vector3 ndir) {
				var forward = Project(p + ndir);
				var backward = Project(p - ndir);
				return (forward - root).Length() + (backward - root).Length();
			}

			for(var i = 0; i < 1000 && Error(dir) > .1f; ++i) {
				var d = Gradient(Error, dir);
				dir = Vector3.Normalize(dir - d * .1f);
			}
			
			return dir;
		}

		static bool IsObscured(Func<Vector3, float> f, Vector3 p) {
			p = FindClosestSurfacePoint(f, p);
			var normal = FirstDerivative(f, p);
			var rd = Vector3.Normalize(p + CameraPos);
			rd = Realign(p, rd);
			if(Vector3.Dot(normal, rd) > 0.3f) return true;
			
			return March(f, (-CameraPos - p).Length() * -rd + p, p) > 0.1f;
		}

		static Vector3 FindLastObscured(Func<Vector3, float> f, Vector3 unobscured, Vector3 obscured) {
			var su = unobscured;
			var so = obscured;
			while((unobscured - obscured).LengthSquared() > .00001f) {
				var mp = Mix(unobscured, obscured, .5f);
				if(IsObscured(f, mp))
					obscured = mp;
				else
					unobscured = mp;
			}
			return obscured;
		}

		static List<List<Vector2>> RemoveHiddenSegments(Func<Vector3, float> f, List<Vector3> path) {
			var opath = path.Select(p => (p, IsObscured(f, p))).ToList();
			var paths = new List<List<Vector2>> { new() };
			Vector3? lpoint = null;
			var lastObscured = false;
			var cpath = paths[0];
			foreach(var (p, obscured) in opath) {
				if(obscured) {
					if(lpoint == null || lastObscured) {
						lpoint = p;
						lastObscured = true;
						continue;
					}
					lpoint = FindLastObscured(f, lpoint.Value, p);
					lastObscured = true;
					cpath.Add(Project(lpoint.Value));
					paths.Add(cpath = new());
					continue;
				}

				if(lastObscured)
					cpath.Add(Project(FindLastObscured(f, p, lpoint.Value)));
				lpoint = p;
				cpath.Add(Project(p));
				lastObscured = false;
			}
			return paths.Where(p => p.Count > 1).ToList();
		}

		static List<List<Vector2>> FindIsoLines(Func<Vector3, float> f, (Vector3, Vector3) bounds, Vector3 planeNormal, float planeOffset) {
			var points = new HashSet<Vector2>();
			
			var (lb, ub) = bounds;
			var cp = (ub - lb) / 2 + lb;
			var bd = (ub - lb).Apply(Max) / 2;
			var xAxis = (planeNormal - Vector3.UnitX).LengthSquared() > Epsilon
				? Vector3.UnitX
				: (planeNormal - Vector3.UnitZ).LengthSquared() > Epsilon
					? Vector3.UnitZ
					: Vector3.UnitY;
			var yAxis = Vector3.Normalize(Vector3.Cross(planeNormal, xAxis));
			var g = f;
			f = p => {
				var d = Vector3.Dot(planeNormal, p) - planeOffset;
				return g(p - planeNormal * d);
			};

			var steps = (int) Ceiling(bd / Resolution / 2);

			var paths = new List<List<Vector2>>();
			var planeOrigin = planeNormal * planeOffset;

			var units = (xAxis, yAxis, planeNormal);

			for(var y = -steps; y <= steps; ++y) {
				var yLine = yAxis * (y * Resolution) + planeOrigin;
				for(var x = -steps; x <= steps; ++x) {
					var p = xAxis * (x * Resolution) + yLine;
					if(!Inside(bounds, p)) continue;
					if(Abs(g(p)) > ResolutionCube || Abs(f(p)) > ResolutionCube) continue;
					
					var path = new List<Vector3>();
					while(true) {
						if(float.IsNaN(p.X)) break;
						p = FindClosestSurfacePoint(f, p);
						if(float.IsNaN(p.X)) break;

						path.Add(p);
						var rp = Project(p).Apply(v => Round(v, 1));
						if(points.Contains(rp)) break;
						points.Add(rp);
						var grad = GradientAlong(g, p, units);
						var normal = Vector3.Normalize(grad);
						if(float.IsNaN(normal.X)) break;
						var cr = Vector3.Normalize(Vector3.Cross(normal, planeNormal));
						var mag = Min(1, Abs(2 - (grad / Epsilon).Length()) * 5);
						if(float.IsNaN(cr.X)) break;
						p += cr * (Resolution * Mix(13.01f, 8f, mag));
					}

					if(path.Count > 2)
						paths.AddRange(RemoveHiddenSegments(g, path));
						//paths.Add(path.Select(Project).ToList());
				}
			}
			
			Console.WriteLine($"Got {paths.Count} isolines!");
			if(paths.Count == 0) return paths;
			var sc = paths.Count;

			//paths = new QuadTree(paths).GetPaths();
			paths = SvgHelper.JoinPaths(paths, 1);
			paths = SvgHelper.ScalePaths(paths, 1000);
			
			paths = SvgHelper.TriviallyJoinPaths(paths);
			paths = SvgHelper.ReorderPaths(paths);
			paths = SvgHelper.SimplifyPaths(paths, 0.25f);
			paths = SvgHelper.ReorderPaths(paths);

			paths = SvgHelper.ScalePaths(paths, 1f / 1000);
			paths = SvgHelper.ReorderPaths(paths);
			
			Console.WriteLine($"{paths.Count} / {sc} after cleanup");

			return paths;
		}

		static bool Inside((Vector3, Vector3) bounds, Vector3 p) {
			var (lb, ub) = bounds;
			return lb.X <= p.X && p.X <= ub.X &&
			       lb.Y <= p.Y && p.Y <= ub.Y &&
			       lb.Z <= p.Z && p.Z <= ub.Z;
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

		static (Vector3 Lower, Vector3 Upper) FindBounds(Func<Vector3, float> sf) {
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

			var lb = new Vector3(
				-FindBound(v => sf(new(-v, sp.Y, sp.Z)), sp.X), 
				-FindBound(v => sf(new(sp.X, -v, sp.Z)), sp.Y), 
				-FindBound(v => sf(new(sp.X, sp.Y, -v)), sp.Z)
			);
			return (lb, 
				new(
					FindBound(v => sf(new(v, sp.Y, sp.Z)), lb.X), 
					FindBound(v => sf(new(sp.X, v, sp.Z)), lb.Y), 
					FindBound(v => sf(new(sp.X, sp.Y, v)), lb.Z)
				)
			);
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

		static float Scene(Vector3 p) => Max(Box(p, Vector3.One), Sphere(p, 1.3f));

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