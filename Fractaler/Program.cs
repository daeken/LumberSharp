using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using Common;
using DotnetNoise;
using DoubleSharp.MathPlus;
using static Common.Helpers;

namespace Fractaler {
	class Program {
		const float Size = 1000;

		static List<Vector2> Circle(float radius, Vector2? center = null, int steps = 100) {
			center ??= Vector2.Zero;
			var path = new List<Vector2>();
			var p = new Vector2(0, radius);
			for(var i = 0; i < steps; ++i)
				path.Add(p.Rotate(i / (float) (steps - 1) * MathF.Tau) + center.Value);
			return path;
		}
		
		static void TileMain(string[] args) {
			var paths = new List<List<Vector2>>();
			var tileset = SvgParser.Load("tiles2.svg");
			var scale = .5f;
			var dscale = scale * 2;
			var tiles = new[] { "#ff00ff", "#ffff00", "#00ffff" }
				.Select(color =>
					tileset.Where(x => x.Color == color).Select(x => x.Path).ToList().Fit(Vector2.One)
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

			SvgHelper.Output("test.svg", paths.Select(x => ("black", x)).ToList(), new());
		}

		static void GreenPillowMain(string[] args) {
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
				paths.AddRange(SvgHelper.SimplifyPaths(new() { line }, .001f));
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
			paths = paths.TriviallyJoinPaths();
			Console.WriteLine("Simplifying paths");
			paths = paths.SimplifyPaths(0.01f);
			Console.WriteLine("Reordering paths");
			paths = paths.ReorderPaths();

			var blackPaths = paths;
			paths = new();

			for(var i = -n / 2; i <= n / 2; ++i) {
				DrawLine(new(-scale, scale / n * 2 * i), new(scale, scale / n * 2 * i));
				DrawLine(new(scale / n * 2 * i, -scale), new(scale / n * 2 * i, scale));
			}

			Console.WriteLine("Reordering paths");
			paths = paths.ReorderPaths();

			SvgHelper.Output("test.svg",
				paths.Select(x => ("green", x)).Concat(blackPaths.Select(x => ("black", x))).ToList(), new());
		}

		static void HeartLimitMain(string[] args) {
			var paths = new List<List<Vector2>>();
			var heart = SvgParser.Load("../Animatrix/heart-outline.svg").Select(x => x.Path).ToList();
			heart = heart.Fit(Vector2.One);
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
					paths.AddRange(heart.Select(path => path.Select(p => P(R(new(x + .5f, y + .5f), p))).ToList()));
			/*for(var i = -bound; i <= bound; ++i) {
				DrawLine(new Vector2(i - offset, -100), new Vector2(i - offset, 100));
				DrawLine(new Vector2(-100, i - offset), new Vector2(100, i - offset));
			}*/
			
			SvgHelper.Output("test.svg", paths.Select(x => ("black", x)).ToList(), new());
		}

		static float[] PentagonAngles(bool offset = false) {
			var angles = new float[5];
			var step = MathF.Tau / 5;
			for(var i = 0; i < 5; ++i)
				angles[i] = step * i + (offset ? step / 2 : 0);
			return angles;
		}

		static Vector2[] PentagonPoints(float radius, bool offset = false) {
			var p = new Vector2(0, -radius);
			return PentagonAngles(offset).Select(a => p.Rotate(a)).ToArray();
		}

		static IEnumerable<Vector2> Arc(float radius, float a, float b, int steps = 100, bool around = false) {
			var p = new Vector2(0, -radius);
			if(around)
				a += MathF.Tau;
			for(var i = 0; i < steps; ++i)
				yield return p.Rotate(Mix(a, b, i / (float) (steps - 1)));
		}

		static IEnumerable<Vector2> ArcI(float radiusA, float radiusB, float a, float b, int steps = 100, bool around = false) {
			if(around)
				a += MathF.Tau;
			for(var i = 0; i < steps; ++i)
				yield return new Vector2(0, -Mix(radiusA, radiusB, i / (float) (steps - 1))).Rotate(Mix(a, b, i / (float) (steps - 1)));
		}

		static List<Vector2> SeraSigil(float radius) {
			var points = PentagonPoints(radius);
			var angles = PentagonAngles();
			var path = new List<Vector2>();
			path.Add(points[3]);
			path.Add(points[0]);
			path.Add(points[2]);
			path.Add(points[4]);
			path.Add(points[1]);
			path.AddRange(Arc(radius, angles[1], angles[3], around: true));
			return path;
		}

		static List<Vector2> SzSigil(float radius) {
			var points = PentagonPoints(radius);
			var iradius = radius * 0.55f;
			var oradius = radius * 1.2f;
			var ipoints = PentagonPoints(iradius, offset: true);
			var angles = PentagonAngles();
			var iangles = PentagonAngles(offset: true);
			var path = new List<Vector2>();
			path.Add(new Vector2(0, -Mix(iradius, radius, 0.75f)).Rotate(Mix(iangles[1], angles[2], 0.75f)));
			for(var i = 5 + 1; i > 1; --i) {
				path.Add(ipoints[i % 5]);
				path.Add(points[i % 5]);
			}
			path.AddRange(ArcI(radius, oradius, angles[2], angles[4], around: true));
			return path;
		}

		static float Clamp(float v, float min, float max) => MathF.Min(MathF.Max(v, min), max);

		static void HeadbondhoodMain(string[] args) {
			var zsigil = SzSigil(1);
			var ssigil = SeraSigil(1);
			var paths = new List<(string Color, List<Vector2> path)>();
			var rng = new Random(570891);

			float Gaussian(float mean = 0, float stdDev = 0) {
				var u1 = 1 - rng.NextSingle();
				var u2 = 1 - rng.NextSingle();
				var rsn = MathF.Sqrt(-2 * MathF.Log(u1)) * MathF.Sin(MathF.Tau * u2);
				return mean + stdDev * rsn;
			}

			bool Overlapping(float ra, Vector2 pa, float rb, Vector2 pb) =>
				Vector2.Distance(pa, pb) <= ra + rb;

			var noise = new FastNoise();
			noise.UsedNoiseType = FastNoise.NoiseType.Perlin;

			var width = 1000;
			var height = 1000;
			var placed = new List<(float Radius, Vector2 Position)>();
			for(var i = 0; i < 100; ++i) {
				var offset = new Vector2(rng.NextSingle() * (width * 0.9f) + (width * 0.05f), MathF.Abs(Gaussian(0f, 0.075f)) * height);
				var scale = Gaussian(7, 3) * 2;
				if(placed.Any(x => Overlapping(x.Radius, x.Position, scale * 2, offset))) {
					i--;
					continue;
				}
				placed.Add((scale * 2, offset));
				var sigil = rng.NextSingle() > 0.5f ? zsigil : ssigil;
				var rot = rng.NextSingle() * MathF.Tau;
				paths.Add(("silver", sigil.Select(p => p.Rotate(rot) * scale + offset).ToList()));
			}

			for(var i = 0; i < 500; ++i) {
				var offset = new Vector2(rng.NextSingle() * (width * 0.9f) + (width * 0.05f), MathF.Abs(Gaussian(0f, 0.075f)) * height);
				var scale = noise.GetNoise(offset.X, offset.Y) * 15;
				if(scale < 1 || placed.Any(x => Overlapping(x.Radius, x.Position, scale * 2, offset))) {
					i--;
					continue;
				}
				placed.Add((scale * 2, offset));
				paths.Add(("silver", Circle(scale, offset)));
			}

			var end = width * 0.8f;
			var soffset = 25;
			for(var j = 0; j < 5; ++j) {
				var spath = new List<Vector2>();
				var zpath = new List<Vector2>();

				for(var i = 0; i < 1000; ++i) {
					var m = i / 999f;
					var h = height * 0.4f + noise.GetNoise(m * 200, j * 50) * 75 + j * 2 + -MathF.Sin(m * MathF.PI) * 50;
					var swap = Mix(1, MathF.Cos(m * 50), MathF.Pow(m, 2));
					spath.Add(new(m * end, h - soffset * swap));
					zpath.Add(new(m * end, h + soffset * swap));
				}

				paths.Add(("purple", spath));
				paths.Add(("green", zpath));
			}

			var fscale = 30;
			paths.Add(("purple", ssigil.Select(x => x.Rotate(MathF.PI / 3) * fscale + new Vector2(end + 20, height * 0.4f - soffset)).ToList()));
			paths.Add(("green", zsigil.Select(x => x.Rotate(MathF.PI / 3) * fscale + new Vector2(end + 40, height * 0.4f - 10f)).ToList()));
			
			for(var i = 0; i < 210; ++i) {
				var waterPath = new List<Vector2>();
				for(var x = 0; x < width; ++x)
					waterPath.Add(new(x, noise.GetNoise(x, i * 10) * 50 + height * 0.5f + i * 2.5f));
				paths.Add(("blue", waterPath));
			}

			paths = paths.Select(x => (x.Color, x.path.SimplifyPath(0.5f))).ToList();
			
			paths.Add(("yellow", new() {
				Vector2.Zero,
				new(width, 0),
				new(width, height),
				new(0, height),
				Vector2.Zero
			}));

			paths = paths.GroupBy(x => x.Color)
				.Select(x => x.Select(y => y.path).ToList().ReorderPaths().Select(y => (x.Key, y)))
				.SelectMany(x => x)
				.ToList();

			SvgHelper.Output("test.svg", paths, new());
		}

		static void GayCarpetMain(string[] args) {
			//var heart = SvgHelper.PathsFromSvg("../Animatrix/heart-outline.svg").Select(x => x.Points).ToList();
			var heart = TextHelper.Render("Gay", "SLF Cue the Music", 20);
			//SvgHelper.Output("test.svg", heart.Select(x => ("black", x)).ToList(), new Page());
			heart = heart.Fit(Vector2.One);
			var paths = Carpet(Vector2.Zero, 0)
				.Select(x => heart.Select(path => ("black", path.Select(p => (p - new Vector2(.5f)) * x.S + x.P).ToList())))
				.SelectMany(x => x)
				.Select(x => (x.Item1, x.Item2.SimplifyPath(0.05f)))
				.ToList();
			SvgHelper.Output("test.svg", paths, new());
		}
		
		static List<List<Vector2>> WarpOrthographic(List<List<Vector2>> paths, Func<Vector2, Vector3> func) =>
			WarpOrthographic(paths, (Vector3 p) => func(new(p.X, p.Y)));

		static List<List<Vector2>> WarpOrthographic(List<List<Vector2>> paths,
			Func<Vector3, Vector3> func
		) =>
			paths.Select(path =>
				path.Select(p => func(new(p, 0)).XY()).ToList()).ToList();

		static Vector3 RotateX(Vector3 p, float a) {
			var r = new Vector2(p.Y, p.Z).Rotate(a);
			return new Vector3(p.X, r.X, r.Y);
		}

		static Vector3 RotateY(Vector3 p, float a) {
			var r = new Vector2(p.X, p.Z).Rotate(a);
			return new Vector3(r.X, p.Y, r.Y);
		}

		static Vector3 RotateZ(Vector3 p, float a) {
			var r = p.XY().Rotate(a);
			return new Vector3(r, p.Z);
		}

		static List<List<Vector2>> RotateXOrthographic(List<List<Vector2>> paths, float anglePerX) =>
			WarpOrthographic(paths, p => RotateX(new Vector3(p, 0), anglePerX * p.X));

		static void SuchGreatHeightsMain() {
			var paths = new List<(string Color, List<Vector2> Path)>();

			var mountainHeight = 1000;
			var mountainBaseWidth = 600;
			var mountainTopWidth = 300;
			var mountainBaseY = 0;
			var mountainSides = new List<(Vector2 Left, Vector2 Right)>();
			var roughness = 10f;
			
			Thread.Sleep(5000);

			var noise = new FastNoise();
			noise.UsedNoiseType = FastNoise.NoiseType.Perlin;
			//noise.Frequency = 10;

			for(var i = 0; i < mountainHeight; ++i) {
				var t = i / (float) (mountainHeight - 1);
				var halfWidth = Mix(mountainBaseWidth, mountainTopWidth, t) / 2;
				var y = i + mountainBaseY;
				var leftOffset = (noise.GetNoise(-halfWidth, y * 10, 0) + 1) * roughness;
				var rightOffset = (noise.GetNoise(halfWidth, y * 10, 0) + 1) * roughness;
				mountainSides.Add((new(-halfWidth - leftOffset, y), new(halfWidth + rightOffset, y)));
			}
			
			paths.Add(("black", mountainSides.Select(x => x.Left).ToList()));
			paths.Add(("black", mountainSides.Select(x => x.Right).ToList()));
			
			SvgHelper.Output("test.svg", paths.Apply(x => x with { Y = -x.Y }), new());
		}

		static void BubbleTextMain(string[] args) {
			var text = TextHelper.Render("Gay", "SLF-OPF Cue the Music", 20);
			text = text.MakeUniformDistances();
			var paths = new List<List<Vector2>>();
			var rng = new Random(1337);
			foreach(var path in text) {
				var len = (int) (path.CalcDrawDistance() * 5);
				for(var i = 0; i < len; ++i)
					paths.Add(Circle(rng.NextSingle() / 5 + 0.2f, path[rng.Next(path.Count)], 25));
			}
			SvgHelper.Output("test.svg", paths, new());
		}

		static void LittleOneHeartMain(string[] args) {
			Visualizer.Run(() => {
				var paths = TextHelper.Render("Little One", "SLF-OPF Cue the Music", 20);
				Console.WriteLine("Rendered text");
				Visualizer.DrawPaths(paths);
				var heart = SvgParser.Load("../Animatrix/heart-outline.svg").Select(x => x.Path).ToList();
				heart = heart.Fit(Vector2.One / 2);
				heart = heart.Apply(p => p - Vector2.One / 4);
				//text = SvgHelper.MakeUniformDistances(text);
				paths = paths.Subdivide(10);
				paths = paths.Fit(Vector2.One);
				//paths = Apply(paths, p => p * 2 - Vector2.One);
				paths = paths.Apply(p => p.Rotate(MathF.Tau / 360 * 90));
				paths = paths.Apply(p => p.Rotate(-p.Length()));
				paths = paths.SimplifyPaths(0.001f);
				paths = paths
					.Concat(paths.Apply(p => p.Rotate(MathF.Tau / 6)))
					.Concat(paths.Apply(p => p.Rotate(MathF.Tau / 6 * 2)))
					.Concat(paths.Apply(p => p.Rotate(MathF.Tau / 6 * 3)))
					.Concat(paths.Apply(p => p.Rotate(MathF.Tau / 6 * 4)))
					.Concat(paths.Apply(p => p.Rotate(MathF.Tau / 6 * 5)))
					.Concat(heart)
					.ToList();
				Visualizer.Clear();
				Visualizer.DrawPaths(paths);
				SvgHelper.Output("test.svg", paths, new());
				Visualizer.WaitForInput();
			});
		}

		static void CardFrontMain(string[] args) {
			Visualizer.Run(() => {
				var paths = new List<(string Color, List<Vector2> Path)>();
				paths.Add(("green", new() {
					new(0, 0), new(10, 0),
					new(10, 7), new(0, 7),
					new(0, 0)
				}));
				paths.Add(("pink", new() {
					new(5, 0), new(5, 7)
				}));
				var heart = SvgParser.Load("../Animatrix/heart-outline.svg").Select(x => x.Path).ToList();
				heart = heart.Fit(Vector2.One * 2, center: true);
				heart = heart.Apply(p => p + new Vector2(5 + 2.5f, 3.5f));
				paths.AddRange(heart.Select(x => ("black", x)));
				var ssigil = new List<List<Vector2>> { SeraSigil(4) };
				ssigil = ssigil.Fit(Vector2.One * 2, center: true);
				ssigil = ssigil.Apply(p => p + new Vector2(5 + 2.5f, 1.25f));
				paths.AddRange(ssigil.Select(x => ("black", x)));
				var zsigil = new List<List<Vector2>> { SzSigil(4) };
				zsigil = zsigil.Fit(Vector2.One * 2, center: true);
				zsigil = zsigil.Apply(p => p + new Vector2(5 + 2.5f, 5.75f));
				paths.AddRange(zsigil.Select(x => ("black", x)));
				var text = TextHelper.Render("Headbondhood", "SLF-OPF Cue the Music", 12);
				text = text.Fit(new Vector2(2.5f, 1) * 0.75f, center: true);
				text = text.Apply(p => p + new Vector2(2.5f, 6.5f));
				//text = text.Subdivide(10);
				paths.AddRange(text.Select(x => ("purple", x)));
				Visualizer.DrawPaths(paths);
				Visualizer.Clear();
				Visualizer.DrawPaths(paths);
				SvgHelper.Output("card_front.svg", paths, new());
				Visualizer.WaitForInput();
			});
		}

		static void ZCardMain(string[] args) {
			Visualizer.Run(() => {
				var paths = new List<(string Color, List<Vector2> Path)>();
				paths.Add(("green", new() {
					new(0, 0), new(10, 0),
					new(10, 7), new(0, 7),
					new(0, 0)
				}));
				paths.Add(("pink", new() {
					new(5, 0), new(5, 7)
				}));

				var offset = 0.25f;
				void AddText(string str, float height) {
					var text = TextHelper.Render(str, "SLF-OPF Cue the Music", height * 50);
					//text = text.ScaleToFit(new Vector2(4, height));
					text = text.Apply(p => p / 50);
					var (lb, ub) = text.GetBounds();
					text = text.Apply(p => p - lb);
					text = text.Apply(p => p + new Vector2(5 + 0.5f, offset));
					text = text.ReorderPaths();
					paths.AddRange(text.Select(x => ("purple", x)));
					offset += height + 0.25f / 4;
				}

				void AddParagraph(string str, float height) {
					var line = new List<string>();
					bool IsOverflow(string word) {
						/*var text = TextHelper.Render(string.Join(" ", line.Append(word)), "SLF-OPF Cue the Music", 20);
						text = text.ScaleToFit(new Vector2(4, height));
						var (lb, ub) = text.GetBounds();
						return ub.X - lb.X > 3.9f;*/
						return string.Join(" ", line.Append(word)).Length > 51;
					}
					foreach(var word in str.Split(' ').Append("**END**")) {
						if(word == "**END**" || IsOverflow(word)) {
							Console.WriteLine($"Found overflow: {string.Join(" ", line)}");
							AddText(string.Join(" ", line), height);
							line.Clear();
						}
						line.Add(word);
					}
				}
				
				AddParagraph("Happy Birthday!", 0.25f);
				AddParagraph("And I'm so, so very proud of you.", 0.25f);
				AddText("", 0.10f);
				AddParagraph("Love,", 0.25f);
				Visualizer.DrawPaths(paths);
				Visualizer.Clear();
				Visualizer.DrawPaths(paths);
				SvgHelper.Output("card_back.svg", paths, new());
				Visualizer.WaitForInput();
			});
		}

		static void MothersDayCardMain(string[] args) {
			Visualizer.Run(() => {
				var paths = new List<(string Color, List<Vector2> Path)>();
				paths.Add(("green", new() {
					new(0, 0), new(10, 0),
					new(10, 7), new(0, 7),
					new(0, 0)
				}));
				paths.Add(("pink", new() {
					new(5, 0), new(5, 7)
				}));

				var offset = 0.25f;
				void AddText(string str, float height) {
					var text = TextHelper.Render(str, "SLF-OPF Cue the Music", height * 50);
					//text = text.ScaleToFit(new Vector2(4, height));
					text = text.Apply(p => p / 50);
					var (lb, ub) = text.GetBounds();
					text = text.Apply(p => p - lb);
					text = text.Apply(p => p + new Vector2(5 + 0.5f, offset));
					text = text.ReorderPaths();
					paths.AddRange(text.Select(x => ("purple", x)));
					offset += height + 0.25f / 4;
				}

				void AddParagraph(string str, float height) {
					var line = new List<string>();
					bool IsOverflow(string word) {
						/*var text = TextHelper.Render(string.Join(" ", line.Append(word)), "SLF-OPF Cue the Music", 20);
						text = text.ScaleToFit(new Vector2(4, height));
						var (lb, ub) = text.GetBounds();
						return ub.X - lb.X > 3.9f;*/
						return string.Join(" ", line.Append(word)).Length > 51;
					}
					foreach(var word in str.Split(' ').Append("**END**")) {
						if(word == "**END**" || IsOverflow(word)) {
							Console.WriteLine($"Found overflow: {string.Join(" ", line)}");
							AddText(string.Join(" ", line), height);
							line.Clear();
						}
						line.Add(word);
					}
				}
				
				Visualizer.DrawPaths(paths);
				Visualizer.Clear();
				Visualizer.DrawPaths(paths);
				SvgHelper.Output("card_back.svg", paths, new());
				Visualizer.WaitForInput();
			});
		}
		
        static void Main(string[] args) {
			Visualizer.Run(() => {
				var paths = new List<(string Color, List<Vector2> Path)>();
				paths.Add(("green", new() {
					new(0, 0), new(10, 0),
					new(10, 7), new(0, 7),
					new(0, 0)
				}));
				paths.Add(("pink", new() {
					new(5, 0), new(5, 7)
				}));

				var offset = 0.25f;
				void AddText(string str, float height) {
					var text = TextHelper.Render(str, "SLF-OPF Cue the Music", height * 50);
					//text = text.ScaleToFit(new Vector2(4, height));
					text = text.Apply(p => p / 50);
					var (lb, ub) = text.GetBounds();
					text = text.Apply(p => p - lb);
					text = text.Apply(p => p + new Vector2(5 + 0.5f, offset));
					text = text.ReorderPaths();
					paths.AddRange(text.Select(x => ("purple", x)));
					offset += height + 0.25f / 4;
				}

				void AddParagraph(string str, float height) {
					var line = new List<string>();
					bool IsOverflow(string word) {
						/*var text = TextHelper.Render(string.Join(" ", line.Append(word)), "SLF-OPF Cue the Music", 20);
						text = text.ScaleToFit(new Vector2(4, height));
						var (lb, ub) = text.GetBounds();
						return ub.X - lb.X > 3.9f;*/
						return string.Join(" ", line.Append(word)).Length > 35;
					}
					foreach(var word in str.Split(' ').Append("**END**")) {
						if(word == "**END**" || IsOverflow(word)) {
							Console.WriteLine($"Found overflow: {string.Join(" ", line)}");
							AddText(string.Join(" ", line), height);
							line.Clear();
						}
						line.Add(word);
					}
				}
				
				Visualizer.DrawPaths(paths);
				Visualizer.Clear();
				Visualizer.DrawPaths(paths);
				Visualizer.WaitForInput();
			});
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
