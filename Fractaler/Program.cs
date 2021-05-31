using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Common;
using static Common.Helpers;

namespace Fractaler {
	class Program {
		const float Size = 1000;
		
		static void TileMain(string[] args) {
			var paths = new List<List<Vector2>>();
			var tileset = SvgHelper.PathsFromSvg("tiles2.svg");
			var scale = .5f;
			var dscale = scale * 2;
			var tiles = new[] { "#ff00ff", "#ffff00", "#00ffff" }
				.Select(color =>
					SvgHelper.Fit(tileset.Where(x => x.Color == color).Select(x => x.Points).ToList(), Vector2.One)
						.Select(path => path.Select(x => new Vector2(x.X * dscale - scale, x.Y * dscale - scale)).ToList().ToList()))
				.ToList();

			Vector2 P(Vector2 p) {
				var vx = MathF.Tanh(p.X / 4);
				var vy = MathF.Tanh(p.Y / 4);
				return new Vector2(vx * MathF.Sqrt(1 - vy * vy / 2), vy * MathF.Sqrt(1 - vx * vx / 2)) * 1000f;
			}

			void DrawLine(Vector2 a, Vector2 b) {
				var steps = 128;
				var step = (b - a) / (steps - 1);
				var line = new List<Vector2>();
				for(var i = 0; i < steps; ++i)
					line.Add(P(a + step * i));
				paths.Add(line);
			}

			var offset = 0;
			var bound = 9;
			for(var x = -bound; x <= bound; ++x)
				for(var y = -bound; y <= bound; ++y)
					paths.AddRange(tiles[Math.Abs(y * x) % 3].Select(path => path.Select(p => P(new Vector2(x + offset, y + offset) + p)).ToList()));
			/*for(var i = -bound; i <= bound; ++i) {
				DrawLine(new Vector2(i - offset, -100), new Vector2(i - offset, 100));
				DrawLine(new Vector2(-100, i - offset), new Vector2(100, i - offset));
			}*/
			
			Console.WriteLine("Joining paths");
			//paths = SvgHelper.TriviallyJoinPaths(paths);

			SvgHelper.Output("test.svg", paths.Select(x => ("black", x)).ToList(), new Page());
		}

		static void Main(string[] args) {
			var paths = new List<List<Vector2>>();
			
			float S(float v) => v / MathF.Sqrt(1 + v*v);

			Vector2 P(Vector2 p) {
				var (vx, vy) = (p / 3).Apply(S);
				return new(vx * MathF.Sqrt(.5f + vy * vy / 2), vy * MathF.Sqrt(.5f + vx * vx / 2));
			}

			void DrawLine(Vector2 a, Vector2 b) {
				var steps = 128;
				var step = (b - a) / (steps - 1);
				var line = new List<Vector2>();
				for(var i = 0; i < steps; ++i)
					line.Add(P(a + step * i));
				paths.AddRange(SvgHelper.SimplifyPaths(new List<List<Vector2>> { line }, .001f));
			}
			
			(int X, int Y) Rot((int X, int Y) p, int n, bool rx, bool ry) {
				if(ry) return p;
				
				var x = p.X;
				var y = p.Y;
				if(rx) {
					x = (n - 1) - x;
					y = (n - 1) - y;
				}

				return (y, x);
			}
			
			Vector2 FromD(int n, int d) {
				var p = (X: 0, Y: 0);
				var t = d;
 
				for(var s = 1; s < n; s <<= 1) {
					var rx = (t & 2) != 0;
					var ry = ((t ^ (rx ? 1 : 0)) & 1) != 0;
					p = Rot(p, s, rx, ry);
					p.X += rx ? s : 0;
					p.Y += ry ? s : 0;
					t >>= 2;
				}
				
				return new Vector2(p.X, p.Y) / new Vector2(n / 2f) - Vector2.One + new Vector2(1f / n);
			}

			var n = 128;
			var scale = 10f;
			for(var i = 0; i < n * n - 1; ++i) {
				DrawLine(FromD(n, i) * scale, FromD(n, i+1) * scale);
			}

			Console.WriteLine("Joining paths");
			paths = SvgHelper.TriviallyJoinPaths(paths);
			Console.WriteLine("Simplifying paths");
			paths = SvgHelper.SimplifyPaths(paths, 0.01f);
			Console.WriteLine("Reordering paths");
			paths = SvgHelper.ReorderPaths(paths);

			var blackPaths = paths;
			paths = new();

			for(var i = -n / 2; i <= n / 2; ++i) {
				DrawLine(new Vector2(-scale, scale / n * 2 * i), new Vector2(scale, scale / n * 2 * i));
				DrawLine(new Vector2(scale / n * 2 * i, -scale), new Vector2(scale / n * 2 * i, scale));
			}

			Console.WriteLine("Reordering paths");
			paths = SvgHelper.ReorderPaths(paths);

			SvgHelper.Output("test.svg",
				paths.Select(x => ("green", x)).Concat(blackPaths.Select(x => ("black", x))).ToList(), new Page());
		}

		static void HeartLimitMain(string[] args) {
			var paths = new List<List<Vector2>>();
			var heart = SvgHelper.PathsFromSvg("../Animatrix/heart-outline.svg").Select(x => x.Points).ToList();
			heart = SvgHelper.Fit(heart, Vector2.One);
			heart = heart.Select(path => path.Select(x => new Vector2(x.X - .5f, x.Y - .5f)).ToList()).ToList();

			Vector2 P(Vector2 p) {
				var r = .03f / p.Length();
				p -= new Vector2(MathF.Sign(p.X) * r, MathF.Sign(p.Y) * r);
				var vx = MathF.Tanh(p.X*p.X);
				var vy = MathF.Tanh(p.Y*p.Y);
				return new(vx * MathF.Sqrt(1 - vy * vy / 2), vy * MathF.Sqrt(1 - vx * vx / 2));
			}

			Vector2 R(Vector2 o, Vector2 p) {
				var a = o.X == 0 && o.Y == 0 ? 0 : MathF.Atan2(o.Y, o.X);
				a += MathF.PI / 2;
				var sa = MathF.Sin(a);
				var ca = MathF.Cos(a);
				return new(
					p.X * ca - p.Y * sa + o.X, 
					p.X * sa + p.Y * ca + o.Y
				);
			}

			void DrawLine(Vector2 a, Vector2 b) {
				var steps = 128;
				var step = (b - a) / (steps - 1);
				var line = new List<Vector2>();
				for(var i = 0; i < steps; ++i)
					line.Add(P(a + step * i));
				paths.Add(line);
			}

			var offset = .5f;
			var bound = 18;
			for(var x = -bound; x <= bound; ++x)
				for(var y = -bound; y <= bound; ++y)
					paths.AddRange(heart.Select(path => path.Select(p => P(R(new Vector2(x + .5f, y + .5f), p))).ToList()));
			/*for(var i = -bound; i <= bound; ++i) {
				DrawLine(new Vector2(i - offset, -100), new Vector2(i - offset, 100));
				DrawLine(new Vector2(-100, i - offset), new Vector2(100, i - offset));
			}*/
			
			SvgHelper.Output("test.svg", paths.Select(x => ("black", x)).ToList(), new Page());
		}
		
		static void MainCarpet(string[] args) {
			var heart = SvgHelper.PathsFromSvg("../Animatrix/heart-outline.svg").Select(x => x.Points).ToList();
			heart = SvgHelper.Fit(heart, Vector2.One);
			var paths = Carpet(Vector2.Zero, 0)
				.Select(x => heart.Select(path => ("black", path.Select(p => (p - new Vector2(.5f)) * x.S + x.P).ToList())))
				.SelectMany(x => x).ToList();
			SvgHelper.Output("test.svg", paths, new Page());
		}

		static IEnumerable<(Vector2 P, float S)> Carpet(Vector2 origin, int level) {
			level++;
			var size = Size / MathF.Pow(3, level);
			yield return (origin, size);
			if(level > 3) yield break;

			foreach(var elem in Carpet(origin + new Vector2(-size, -size), level)) yield return elem;
			foreach(var elem in Carpet(origin + new Vector2(-size, 0    ), level)) yield return elem;
			foreach(var elem in Carpet(origin + new Vector2(-size,  size), level)) yield return elem;
			foreach(var elem in Carpet(origin + new Vector2( size, -size), level)) yield return elem;
			foreach(var elem in Carpet(origin + new Vector2( size, 0    ), level)) yield return elem;
			foreach(var elem in Carpet(origin + new Vector2( size,  size), level)) yield return elem;
			foreach(var elem in Carpet(origin + new Vector2(0    , -size), level)) yield return elem;
			foreach(var elem in Carpet(origin + new Vector2(0    ,  size), level)) yield return elem;
		}
	}
}