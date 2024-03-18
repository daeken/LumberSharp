using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Common;
using DoubleSharp.MathPlus;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using static Common.Helpers;

namespace Demarkate {
	class Marker {
		public Vector2 TopLeft, TopRight, BottomLeft, BottomRight;
		public ushort Data;
	}
	
	class Program {
		static readonly List<(Vector2, Color)> Reticles = new();
		static readonly List<(Vector2, Vector2, Color)> Lines = new();
		static readonly long[] CodeMap = { 0, 0b01, 0b10, 0, 0b00, 0, 0, 0b11 };

		static void Main(string[] args) {
			using var im = (Image<Rgba32>) Image.Load(args[0]);

			Rgba32 GetPixel(Vector2 p) {
				var a = im[(int) MathF.Floor(p.X), (int) MathF.Floor(p.Y)];
				var b = im[(int) MathF.Ceiling(p.X), (int) MathF.Ceiling(p.Y)];
				var t = p - p.Apply(MathF.Truncate);
				var c1 = Mix(a.ToVector4(), b.ToVector4(), t.X);
				var c2 = Mix(a.ToVector4(), b.ToVector4(), t.Y);
				return new Rgba32((c1 + c2) / 2);
			}
			
			foreach(var marker in FindMarkers(im)) {
				Console.WriteLine($"Sampling frame {marker.Data}");
				var nw = (int) MathF.Ceiling(MathF.Max((marker.TopLeft - marker.TopRight).Length(), (marker.BottomLeft - marker.BottomRight).Length())) * 2;
				var nh = (int) MathF.Ceiling(MathF.Max((marker.TopLeft - marker.BottomLeft).Length(), (marker.TopRight - marker.BottomRight).Length())) * 2;
				var xstep = 1f / (nw - 1);
				var ystep = 1f / (nh - 1);
				var nim = new Image<Rgba32>(nw, nh);
				for(var y = 0; y < nh; ++y)
					for(var x = 0; x < nw; ++x) {
						var po = new Vector2(x * xstep, y * ystep);
						var s1 = Mix(marker.TopLeft, marker.TopRight, po.X);
						var s2 = Mix(marker.BottomLeft, marker.BottomRight, po.X);
						nim[x, y] = GetPixel(Mix(s1, s2, po.Y));
					}
				nim.Mutate(x => x.Resize(int.Parse(args[1]), int.Parse(args[2]), KnownResamplers.Lanczos3));
				nim.Save($"frame{marker.Data:00000}.png");
				Console.WriteLine($"Done with frame {marker.Data}");
			}

			var mim = im.Clone();

			void Set(int x, int y, Color color) {
				if(!(x < 0 || x >= mim.Width || y < 0 || y >= mim.Height)) mim[x, y] = color;
			}

			foreach(var (a, b, color) in Lines) {
				var dir = Vector2.Normalize(b - a) / 2;
				var steps = (int) MathF.Round((b - a).Length() * 2);
				for(var i = 0; i < steps; ++i) {
					var (px, py) = a + dir * i;
					var x = (int) MathF.Round(px);
					var y = (int) MathF.Round(py);
					Set(x, y, color);
				}
			}

			foreach(var (p, color) in Reticles) {
				var (px, py) = p;
				var x = (int) MathF.Round(px);
				var y = (int) MathF.Round(py);
				for(var i = -10; i <= 10; ++i) {
					Set(x + i, y, color);
					Set(x, y + i, color);
				}
			}

			mim.Save(args[0] + ".reticle.png");
		}

		static void AddReticle(Vector2 v, Color color) => Reticles.Add((v, color));
		static void AddLine(Vector2 a, Vector2 b, Color color) => Lines.Add((a, b, color));
		
		static bool[,] Binarize(Image<Rgba32> im) {
			Console.WriteLine(im.Width);
			var ret = new bool[im.Width, im.Height];
			for(var x = 0; x < im.Width; ++x)
				for(var y = 0; y < im.Height; ++y) {
					var p = im[x, y];
					ret[x, y] = Math.Max(p.R, Math.Max(p.G, p.B)) < 128;
				}

			return ret;
		}

		static IEnumerable<List<(int X, int Y)>> FindEnclosedRegions(bool[,] bits) {
			var (width, height) = (bits.GetLength(0), bits.GetLength(1));
			var bad = new bool[width, height];
			for(var x = 0; x < width; ++x) {
				bad[x, 0] = true;
				bad[x, height - 1] = true;
			}
			for(var y = 0; y < height; ++y) {
				bad[0, y] = true;
				bad[width - 1, y] = true;
			}

			var checking = new int[width, height];

			var iter = -1;
			for(var x = 1; x < width - 1; ++x) {
				for(var y = 1; y < height - 1; ++y, ++iter) {
					if(bits[x, y] || bad[x, y]) continue;
					var discard = false;
					var pixels = new HashSet<(int, int)>();
					var queue = new Queue<(int, int)>();
					queue.Enqueue((x, y));
					checking[x, y] = iter;

					while(queue.TryDequeue(out var t)) {
						var (tx, ty) = t;
						if(bad[tx, ty]) {
							discard = true;
							break;
						}

						if(bits[tx, ty]) continue;
						pixels.Add(t);
						void Check(int cx, int cy) {
							if(checking[cx, cy] == iter) return;
							checking[cx, cy] = iter;
							queue.Enqueue((cx, cy));
						}
						Check(tx - 1, ty - 1);
						Check(tx - 1, ty);
						Check(tx - 1, ty + 1);
						Check(tx, ty - 1);
						Check(tx, ty + 1);
						Check(tx + 1, ty - 1);
						Check(tx + 1, ty);
						Check(tx + 1, ty + 1);
					}

					foreach(var v in pixels) bad[v.Item1, v.Item2] = true;
					if(!discard && pixels.Count > 2)
						yield return pixels.ToList();
				}
			}
		}

		static int CountPointsOnSide(Vector2 a, Vector2 b, List<Vector2> points) =>
			points.Count(p => (p.X - a.X) * (b.Y - a.Y) - (p.Y - a.Y) * (b.X - a.X) < 0);

		static IEnumerable<Marker> FindMarkers(Image<Rgba32> im) {
			var bits = Binarize(im);

			var regions = new List<List<(int X, int Y)>> { new() }.Concat(FindEnclosedRegions(bits)).ToList();
			var regionMap = new int[im.Width, im.Height];
			Parallel.ForEach(regions, (r, _, i) => r.ForEach(p => regionMap[p.X, p.Y] = (int) i));

			var markers = new Dictionary<(int Data, bool IsTop), (Vector2 A, Vector2 B, Vector2 C, Vector2 D)>();
			
			foreach(var region in regions) {
				if(region.Count is < 50 or > 250000) continue;
				//if(!region.Contains((2430, 1415))) continue;
				var regionVectors = region.SelectList(x => x.ToVector());
				var centroid = regionVectors.Select(p => p / regionVectors.Count).Aggregate((a, b) => a + b);
				var furthest = regionVectors.OrderByDescending(p => (p - centroid).Length()).Take(250)
					.ToList();
				var xbound = regionVectors.Select(v => MathF.Abs(v.X - centroid.X)).OrderByDescending(v => v).First();
				var ybound = regionVectors.Select(v => MathF.Abs(v.Y - centroid.Y)).OrderByDescending(v => v).First();
				var bound = MathF.Max(xbound, ybound);
				var hbound = bound / 2;
				var most = (int) (region.Count * .99f);
				var (la, lb) = furthest.Select(a => furthest.Select(b => (a, b))).SelectMany(x => x).AsParallel()
					.Where(p => CountPointsOnSide(p.a, p.b, furthest) >= 200)
					.Where(p => CountPointsOnSide(p.a, p.b, regionVectors) >= most)
					.AsSequential().OrderByDescending(x => (x.a - x.b).Length()).FirstOrDefault();

				var lcd = (regionVectors.AsParallel().Where(g => (g - la).Length() >= hbound).OrderByDescending(g => CountPointsOnSide(g, la, regionVectors))
					.First() - la).Normalize();
				var ldd = (regionVectors.AsParallel().Where(g => (g - lb).Length() >= hbound).OrderByDescending(g => CountPointsOnSide(lb, g, regionVectors))
					.First() - lb).Normalize();
				
				var bottom = hbound;
				var top = bound * 4;
				for(var steps = 0; steps < 100 && top - bottom > .5f; ++steps) {
					var middle = (top + bottom) / 2;
					var tc = la + lcd * middle;
					var td = lb + ldd * middle;
					var count = CountPointsOnSide(td, tc, regionVectors);
					if(count >= region.Count - 2) top = middle;
					else bottom = middle;
				}

				if(top - bottom > 1) continue;

				var lc = la + lcd * top;
				var ld = lb + ldd * top;

				lb = regionVectors.OrderByDescending(p => (lc - p).Length()).First();

				ldd = (regionVectors.AsParallel().Where(g => (g - lb).Length() >= hbound).OrderByDescending(g => CountPointsOnSide(lb, g, regionVectors))
					.First() - lb).Normalize();
				ld = lb + ldd * top;

				var scale = 56;

				var le = (lb - la) / 7 * scale + la;
				var lf = (ld - lc) / 7 * scale + lc;
				
				var xSpace = 1f / scale;
				var xOff = xSpace / 2;
				var ySpace = 1f / 7;
				var yOff = ySpace / 2;

				(int, int) GetPoint(int gx, int gy) {
					var tx = xSpace * gx + xOff;
					var ty = ySpace * gy + yOff;
					var s1 = Mix(la, le, tx);
					var s2 = Mix(lc, lf, tx);
					var (px, py) = Mix(s1, s2, ty);
					return ((int) MathF.Round(px), (int) MathF.Round(py));
				}
				
				List<(int, int)> GetRegion(int gx, int gy) {
					var (rx, ry) = GetPoint(gx, gy);
					return regions[(rx < 0 || rx >= im.Width || ry < 0 || ry >= im.Height) ? 0 : regionMap[rx, ry]];
				}

				Vector2 FindCenterLine(Vector2 inside, Vector2 opposite) {
					var dir = Vector2.Normalize(inside - opposite) / 2;
					var p = inside;
					while(!bits[(int) MathF.Round(p.X), (int) MathF.Round(p.Y)])
						p += dir;
					inside = p;
					while(bits[(int) MathF.Round(p.X), (int) MathF.Round(p.Y)])
						p += dir;
					return (p + inside) / 2;
				}
				
				AddReticle(la, Color.Red);
				AddReticle(lb, Color.Green);
				AddReticle(lc, Color.Blue);
				AddReticle(ld, Color.Yellow);

				try {
					lc = GetRegion(0, 6).Select(p => p.ToVector()).OrderByDescending(p => (lb - p).Length()).First();
					ld = GetRegion(6, 6).Select(p => p.ToVector()).OrderByDescending(p => (la - p).Length()).First();
					la = regionVectors.OrderByDescending(p => (ld - p).Length()).First();
					lb = regionVectors.OrderByDescending(p => (lc - p).Length()).First();
					
					la = FindCenterLine(la, ld);
					lb = FindCenterLine(lb, lc);
					lc = FindCenterLine(lc, lb);
					ld = FindCenterLine(ld, la);
				} catch(Exception) {
					continue;
				}

				le = (lb - la) / 7 * scale + la;
				lf = (ld - lc) / 7 * scale + lc;

				var grid = new BitGrid(region, (7, 7), la, lb, lc, ld);
				if(!grid[0, 0]) continue;
				var value = 0L;
				for(var y = 0; y < 7; ++y)
					for(var x = 0; x < 7; ++x)
						value = (value << 1) | (grid[x, y] ? 1 : 0);
				var expected = 0b1010101_1110111_0100010_1111111_1001001_0011100_0001000L;
				Console.WriteLine($"Marker: {Convert.ToString(value, 2)}");
				if(value != expected) continue;
				
				Console.WriteLine($"FOUND MARKER! {la} {ld}");

				var bcv = Enumerable.Range(0, 24).SelectList(i => {
					var gx = (i * 2) + 8;
					var gr = GetRegion(gx, 6);
					var v = 8 - Enumerable.Range(1, 7).FirstOrDefault(h => gr.Contains(GetPoint(gx, h - 1)));
					
					float CalcError() {
						var ec = (GetPoint(gx, 6).ToVector() + GetPoint(gx, 7 - v).ToVector()) / 2;
						var rc = gr.Select(p => p.ToVector()).Centroid();
						return (ec - rc).Length();
					}

					var td = Vector2.Normalize(le - la) / 25;
					var bd = Vector2.Normalize(lf - lc) / 25;
					var (ole, olf) = (le, lf);
					(le, lf, _) = Enumerable.Range(-200, 401).Select(j => {
						le = ole + j * td;
						lf = olf + j * bd;
						return (le, lf, CalcError());
					}).OrderBy(x => x.Item3).First();
					return v;
				});
				
				if(bcv.Contains(8)) Console.WriteLine("Bad bar values!");

				var code = bcv.Select((x, i) => CodeMap[x] << (i * 2)).Sum();

				var eci = 0b01_01_01_01_01_01_01_01__11_10_01_00_00_01_10_11L;
				var isTop = true;
				if(code >> 16 != eci) {
					code ^= (1L << 48) - 1;
					isTop = false;
					if(code >> 16 != eci)
						Console.WriteLine($"Mismatch -- expected {Convert.ToString(eci, 2)} but got {Convert.ToString(code >> 16, 2)}");
				}
				
				markers[((int) (code & 0xFFFF), isTop)] = (la, lb, lc, ld);
			}

			var dataset = markers.Keys.Select(k => k.Data).Distinct().ToList();
			foreach(var elem in dataset) {
				if(!markers.ContainsKey((elem, true)) || !markers.ContainsKey((elem, false))) {
					Console.WriteLine($"Missing one marker for data {elem}");
					continue;
				}

				var (ta, tb, tc, td) = markers[(elem, true)];
				var (ba, bb, bc, bd) = markers[(elem, false)];

				Vector2 JustOutside(Vector2 a, Vector2 b, Vector2 c, Vector2 d) {
					var t1 = (a + b) / 2;
					var t2 = (c + d) / 2;
					return Mix(t1, t2, 1 + 1f / 7);
				}
				
				var tt = JustOutside(ta, tb, tc, td);
				var bt = JustOutside(ba, bb, bc, bd);
				var tr = regionMap[(int) MathF.Round(tt.X), (int) MathF.Round(tt.Y)];
				var br = regionMap[(int) MathF.Round(bt.X), (int) MathF.Round(bt.Y)];
				if(tr != br) {
					Console.WriteLine("Marker target region mismatch!");
					continue;
				}

				var rv = regions[tr].SelectList(v => v.ToVector());
				var rc = rv.Centroid();
				var md = MathF.Max((rc - tc).Length(), (rc - bc).Length());
				var fa = rv.Where(p => (p - tc).Length() < md / 2).Select(p => (p, (p - rc).Length())).OrderByDescending(p => p.Item2).First().p;
				var fd = rv.Select(p => (p, (p - fa).Length())).OrderByDescending(p => p.Item2).First().p;
				var fb = rv.Select(p => (p, (p - fa).Length() + (p - fd).Length())).OrderByDescending(p => p.Item2).First().p;
				var fc = rv.Select(p => (p, (p - fb).Length())).OrderByDescending(p => p.Item2).First().p;

				var tg = Mix(ta, tb, 1000);
				if((fc - tg).Length() < (fb - tg).Length())
					(fb, fc) = (fc, fb);

				var fcent = new[] { fa, fb, fc, fd }.Centroid();
				fa += Vector2.Normalize(fcent - fa) * 5;
				fb += Vector2.Normalize(fcent - fb) * 5;
				fc += Vector2.Normalize(fcent - fc) * 5;
				fd += Vector2.Normalize(fcent - fd) * 5;
				AddReticle(fa, Color.Cyan);
				AddReticle(fb, Color.Cyan);
				AddReticle(fc, Color.Cyan);
				AddReticle(fd, Color.Cyan);

				yield return new Marker { TopLeft = fa, TopRight = fb, BottomLeft = fc, BottomRight = fd, Data = (ushort) elem};
			}

			yield break;
		}
	}
}