﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Common;
using static Common.Helpers;
using static System.MathF;
using static System.Numerics.Vector3;

namespace TriContour {
	class Program {
		const float Epsilon = 0.001f;
		const float Resolution = 0.01f;
		static readonly float ResolutionSquare = Sqrt(Resolution*Resolution*2);

		const float NearPlane = 1;
		const float FarPlane = 1000;
		static Matrix4x4 Projection = Matrix4x4.CreatePerspectiveFieldOfView(Tau / 4, 1, NearPlane, FarPlane);
		static Vector3 CameraPos;
		static Matrix4x4 View;
		static Matrix4x4 ViewProject;

		static int Frame;

		static void Main(string[] args) {
			var bounds = (new Vector3(-5), new Vector3(5)); // FindBounds(Scene);

			var axes = new[] {
				UnitX, 
				UnitY, 
				UnitZ
			};

			var center = (Vector2?) null;

			var rot = Tau / 4 / 5;

			var ap = new List<(string, List<Vector2>)>();

			for(Frame = 0; Frame < 30; ++Frame) {
				var crot = new Vector2(0, -5).Rotate(Frame * rot);
				CameraPos = new(crot.X, -5f, crot.Y);
				View = Matrix4x4.CreateLookAt(CameraPos, new Vector3(0.0001f, 0.0001f, 0.0001f), UnitY);
				ViewProject = View * Projection;
				
				var paths = axes.Select(axis => Enumerable.Range(-12, 25).Select(x => x / 3f).Select(d => (axis, d)))
					.SelectMany(x => x)
					.AsParallel().Select(x => FindIsoLines(Scene, bounds, x.axis, x.d))
					.SelectMany(x => x).ToList();

				paths = SvgHelper.ScalePaths(paths, 250);
				//paths = SvgHelper.Fit(paths, new(1000));
				Console.WriteLine($"Got {paths.Count} total isolines!");

				Console.WriteLine("ReorderPaths");
				paths = SvgHelper.ReorderPaths(paths);

				Console.WriteLine($"{paths.Count} after cleanup");
				Console.WriteLine($"{SvgHelper.CalcDrawDistance(paths)} total units drawn");

				if(center == null) {
					var pbounds = SvgHelper.GetBounds(paths);
					center = (pbounds.Upper + pbounds.Lower) / 2;
				}

				paths = paths.Select(path => path.Select(p => p - center.Value).ToList()).ToList();

				var frame = Fiducial.CreateFrame(new(2000, 1300), (ushort) Frame);

				ap.AddRange(paths.Select(x => ($"black###Frame{Frame+1}", x)).Concat(new[] { ($"green###Frame{Frame+1}", frame) }).Select(path => (path.Item1, path.Item2.Select(x => x + new Vector2(0, 2000 * Frame)).ToList())).ToList());
			}
			
			SvgHelper.Output("frames_boxsphere.svg", ap, new Page { Width = 2100 / 10, Height = 2000 * Frame / 10 });
		}

		static Vector2 Project(Vector3 point) {
			var p4 = Vector4.Transform(point, ViewProject);
			return new Vector2(p4.X, p4.Y) / p4.W * 10;
		}

		static float March(Func<Vector3, float> f, Vector3 from, Vector3 to, int steps = 256) {
			var rd = Normalize(to - from);
			var t = 0f;
			for(var i = 0; i < steps; ++i) {
				var p = from + rd * t;
				var d = f(p);
				if(Abs(d) <= Epsilon)
					return t / (to - from).Length();
				t += d * 0.9f;
			}
			return 1;
		}

		static bool IsObscured(Func<Vector3, float> f, Vector3 p) {
			p = FindClosestSurfacePoint(f, p);
			var normal = FirstDerivative(f, p);
			var rd = Normalize(p - CameraPos);
			return March(f, CameraPos, p) < 0.999f;
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

		static List<List<Vector3>> RemoveHiddenSegments(Func<Vector3, float> f, List<Vector3> path) {
			var opath = path.Select(p => (p, IsObscured(f, p))).ToList();
			var paths = new List<List<Vector3>> { new() };
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
					cpath.Add(lpoint.Value);
					paths.Add(cpath = new());
					continue;
				}

				if(lastObscured)
					cpath.Add(FindLastObscured(f, p, lpoint.Value));
				lpoint = p;
				cpath.Add(p);
				lastObscured = false;
			}
			return paths.Where(p => p.Count > 1).ToList();
		}

		static IEnumerable<Vector3> Subdivide(Func<Vector3, float> f, List<Vector3> path) {
			if(path.Count <= 1) yield break;

			var res = 0.0005f;
			
			var lp = FindClosestSurfacePoint(f, path.First());
			yield return lp;
			foreach(var _np in path.Skip(1)) {
				var np = FindClosestSurfacePoint(f, _np);
				var d = np - lp;
				var steps = (int) Ceiling(d.Length() / res);
				var chunk = d / (steps + 1);
				for(var i = 0; i < steps - 1; ++i)
					yield return FindClosestSurfacePoint(f, lp + chunk * i);
				yield return np;
				lp = np;
			}
		}

		static List<List<Vector3>> Join(Func<Vector3, float> f, List<List<Vector3>> paths) {
			const float maxDist = 0.01f;
			const float contFactor = Epsilon;
			const int contSteps = 5;
			
			bool IsContinuous(Vector3 a, Vector3 b) {
				var step = (b - a) / (contSteps + 2);
				for(var i = 1; i <= contSteps; ++i)
					if(Abs(f(a + step * i)) > contFactor) return false;
				return true;
			}

			float Dist(Vector3 a, Vector3 b) => (b - a).Length();

			while(paths.Count > 1) {
				var comb = false;

				void Comb(int i, int j, bool swapA, bool swapB) {
					var a = paths[i];
					var b = paths[j];
					paths.RemoveAt(Math.Max(i, j));
					paths.RemoveAt(Math.Min(i, j));
					if(swapA) a.Reverse();
					if(swapB) b.Reverse();
					paths.Add(a.Concat(b).ToList());
					comb = true;
				}
				
				for(var i = 0; i < paths.Count && !comb; ++i) {
					var a = paths[i];
					var aa = a[0];
					var ab = a[^1];
					for(var j = i + 1; j < paths.Count && !comb; ++j) {
						var b = paths[j];
						var ba = b[0];
						var bb = b[^1];

						if(Dist(ab, ba) <= maxDist && IsContinuous(ab, ba)) Comb(i, j, false, false);
						else if(Dist(aa, bb) <= maxDist && IsContinuous(aa, bb)) Comb(j, i, false, false);
						else if(Dist(ab, bb) <= maxDist && IsContinuous(ab, bb)) Comb(i, j, false, true);
						else if(Dist(aa, ba) <= maxDist && IsContinuous(aa, ba)) Comb(i, j, true, false);
					}
				}

				if(!comb) return paths;
			}
			return paths;
		}

		static List<List<Vector2>> FindIsoLines(Func<Vector3, float> f3, (Vector3, Vector3) bounds, Vector3 planeNormal, float planeOffset) {
			var points = new HashSet<Vector2>();
			
			var (lb, ub) = bounds;
			var bd = (ub - lb).Apply(Max) / 2;
			var planeOrigin = planeNormal * planeOffset;
			var xAxis = (planeNormal - UnitX).LengthSquared() > Epsilon
				? UnitX
				: (planeNormal - UnitZ).LengthSquared() > Epsilon
					? UnitZ
					: UnitY;
			var yAxis = Normalize(Cross(planeNormal, xAxis));
			var UpDimension = (Func<Vector2, Vector3>) (p => p.X * xAxis + p.Y * yAxis + planeOrigin);
			var f = UpDimension.Compose(f3);

			var steps = (int) Ceiling(bd / Resolution / 2);

			var paths = new List<List<Vector2>>();

			for(var y = -steps; y <= steps; ++y) {
				for(var x = -steps; x <= steps; ++x) {
					var p = new Vector2(x * Resolution, y * Resolution);
					if(!Inside(bounds, UpDimension(p))) continue;
					if(Abs(f(p)) > ResolutionSquare) continue;
					
					var path = new List<Vector2>();
					var lastNormal = (Vector2?) null;
					while(true) {
						p = FindClosestSurfacePoint(f, p);
						path.Add(p);

						var rp = p.Apply(v => Round(v, 2));
						if(points.Contains(rp)) break;
						points.Add(rp);
						var normal = FirstDerivative(f, p);
						if(float.IsNaN(normal.X)) {
							if(lastNormal == null) break;
							normal = lastNormal.Value;
						}
						lastNormal = normal;
						p += normal.Rotate(Tau / 4) * (Resolution / 2);
					}

					if(path.Count > 1)
						paths.Add(path);
				}
			}

			Console.WriteLine($"Got {paths.Count} isolines!");
			if(paths.Count == 0) return new();
			var sc = paths.Count;

			paths = SvgHelper.ScalePaths(paths, 100);
			paths = SvgHelper.TriviallyJoinPaths(paths);
			paths = SvgHelper.ReorderPaths(paths);
			paths = SvgHelper.JoinPaths(paths);
			paths = SvgHelper.SimplifyPaths(paths, 0.1f);
			paths = SvgHelper.ReorderPaths(paths);
			paths = SvgHelper.JoinPaths(paths);
			paths = SvgHelper.ScalePaths(paths, 1f / 100);
			paths = SvgHelper.ReorderPaths(paths);

			var tpaths = paths.Select(path => path.Select(UpDimension).ToList()).ToList();
			tpaths = tpaths.Select(path => Subdivide(f3, path).ToList()).ToList();
			tpaths = tpaths.Select(path => RemoveHiddenSegments(f3, path)).SelectMany(x => x).ToList();
			
			paths = tpaths.Select(path => path.Select(Project).ToList()).ToList();

			paths = SvgHelper.ScalePaths(paths, 100);
			paths = SvgHelper.TriviallyJoinPaths(paths);
			paths = SvgHelper.ReorderPaths(paths);
			paths = SvgHelper.JoinPaths(paths);
			paths = SvgHelper.SimplifyPaths(paths, 0.1f);
			paths = SvgHelper.ReorderPaths(paths);
			paths = SvgHelper.JoinPaths(paths);
			paths = SvgHelper.ScalePaths(paths, 1f / 100);
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

		static Vector2 FindClosestSurfacePoint(Func<Vector2, float> f, Vector2 p) {
			for(var i = 0; i < 10000; ++i) {
				var d = f(p);
				if(d is >= 0 and <= Epsilon) return p;
				if(d < 0)
					p -= FirstDerivative(f, p) * (d * 1.5f);
				else
					p -= FirstDerivative(f, p) * (d / 2);
			}
			return p;
		}
		
		static Vector3 FindClosestSurfacePoint(Func<Vector3, float> f, Vector3 p) {
			for(var i = 0; i < 10000; ++i) {
				var d = f(p);
				if(d is >= 0 and <= Epsilon) return p;
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

		static Vector2 FirstDerivative(Func<Vector2, float> f, Vector2 p) =>
			Vector2.Normalize(Gradient(f, p));
		
		static Vector2 Gradient(Func<Vector2, float> f, Vector2 p) =>
			new(
				f(new(p.X + Epsilon, p.Y)) - f(new(p.X - Epsilon, p.Y)),
				f(new(p.X, p.Y + Epsilon)) - f(new(p.X, p.Y - Epsilon)) 
			);
		
		static Vector3 FirstDerivative(Func<Vector3, float> f, Vector3 p) =>
			Normalize(Gradient(f, p));

		static Vector3 Gradient(Func<Vector3, float> f, Vector3 p) =>
			new(
				f(new(p.X + Epsilon, p.Y, p.Z)) - f(new(p.X - Epsilon, p.Y, p.Z)),
				f(new(p.X, p.Y + Epsilon, p.Z)) - f(new(p.X, p.Y - Epsilon, p.Z)), 
				f(new(p.X, p.Y, p.Z + Epsilon)) - f(new(p.X, p.Y, p.Z - Epsilon))
			);
		
		static Vector3 FirstDerivativeAlong(Func<Vector3, float> f, Vector3 p, (Vector3 X, Vector3 Y, Vector3 Z) u) =>
			Normalize(GradientAlong(f, p, u));

		static Vector3 GradientAlong(Func<Vector3, float> f, Vector3 p, (Vector3 X, Vector3 Y, Vector3 Z) u) =>
			(f(p + Epsilon * u.X) - f(p - Epsilon * u.X)) * u.X + 
			(f(p + Epsilon * u.Y) - f(p - Epsilon * u.Y)) * u.Y +
			(f(p + Epsilon * u.Z) - f(p - Epsilon * u.Z)) * u.Z;

		static float Scene(Vector3 p) => Subtract(Box(p, One * 1f), Sphere(p, 1 + Max(-.02f, Min((Frame - 5) * .01f, .2f))));//, Sphere(p + One * .5f, 1.2f));

		static float Union(params float[] objs) => objs.Min();
		static float Intersect(params float[] objs) => objs.Max();
		static float Subtract(params float[] objs) => objs.Take(1).Concat(objs.Skip(1).Select(x => -x)).Max();

		static float Twist(Vector3 p, Vector3 axis, float angle, Func<Vector3, float> f) =>
			f(Transform(p, Matrix4x4.CreateFromAxisAngle(axis, angle)));

		static float Plane(Vector3 p, Vector3 n, float h) => Dot(p, n) + h;
		
		static float Sphere(Vector3 p, float r) => p.Length() - r;

		static float Box(Vector3 p, Vector3 b) {
			var q = p.Apply(Abs) - b;
			return Max(q, Zero).Length() + Min(q.Apply(Max), 0);
		}
	}
}