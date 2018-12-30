using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Lightness.Renderer;
using PrettyPrinter;

namespace Lightness {
	public class Vectorize {
		readonly Pixel[] Pixels;
		readonly int Width, Height;
		
		readonly List<List<(int, int)>> FinalPaths;
		
		public Vectorize(Pixel[] pixels, int width, int height, bool edgePreview) {
			Pixels = pixels;
			Width = width;
			Height = height;
			
			FindDepthDelta();
			if(edgePreview) return;
			
			RemoveNonEdges();
			FloodFill();
			RemoveNoise();
			var paths = Trace();
			paths = TriviallyJoinPaths(paths);
			paths = SimplifyPaths(paths);
			paths = ReorderPaths(paths);
			paths = JoinPaths(paths);
			paths = SimplifyPaths(paths);
			paths = ReorderPaths(paths);
			FinalPaths = paths;
		}

		static readonly (int, int)[] Neighbors = {
			(-1, -1), (0, -1), (1, -1), 
			(-1, 0), /*home*/ (1, 0), 
			(-1, 1), (0, 1), (1, 1)
		};
		
		Pixel sample(int x, int y) => x < 0 || x >= Width || y < 0 || y >= Height ? null : Pixels[y * Width + x];
		IEnumerable<Pixel> sampleNeighbors(int x, int y) =>
			Neighbors.Select(t => sample(x + t.Item1, y + t.Item2));

		void FindDepthDelta() {
			"Finding depth deltas".Debug();
			for(int y = 0, i = 0; y < Height; ++y)
				for(var x = 0; x < Width; ++x, ++i) {
					var pixel = Pixels[i];
					if(pixel == null) continue;

					var neighborDepthDeltas = sampleNeighbors(x, y).Select(n => n == null ? float.PositiveInfinity : MathF.Abs(pixel.Depth - n.Depth));
					pixel.DepthDelta = neighborDepthDeltas.Max();
					var neighborAngleDeltas = sampleNeighbors(x, y).Select(n => n == null ? MathF.PI : MathF.Abs(MathF.Acos(Vector3.Dot(pixel.Normal, n.Normal))));
					pixel.AngleDelta = neighborAngleDeltas.Max();
					pixel.Edge = pixel.DepthDelta == float.PositiveInfinity || pixel.DepthDelta > 0.00005f || pixel.AngleDelta >= MathF.PI / 4;
				}
		}

		void RemoveNonEdges() {
			for(var i = 0; i < Pixels.Length; ++i)
				if(Pixels[i] != null && !Pixels[i].Edge)
					Pixels[i] = null;
		}
		
		readonly List<List<(int, int)>> Patches = new List<List<(int, int)>>();

		void Flood(int sx, int sy) {
			var locs = new List<(int, int)>();
			Patches.Add(locs);
			var queue = new Queue<(float, int, int)>();
			queue.Enqueue((Pixels[sy * Width + sx].Depth, sx, sy));

			while(queue.Count != 0) {
				var (pdepth, x, y) = queue.Dequeue();
				var i = y * Width + x;
				if(x < 0 || x >= Width || y < 0 || y >= Height || Pixels[i] == null || Pixels[i].Flooded) continue;
				var depth = Pixels[i].Depth;
				if(MathF.Abs(pdepth - depth) > 1) continue;
				locs.Add((x, y));
				Pixels[i].Flooded = true;
				queue.Enqueue((depth, x - 1, y - 1));
				queue.Enqueue((depth, x    , y - 1));
				queue.Enqueue((depth, x + 1, y - 1));
				queue.Enqueue((depth, x - 1, y    ));
				queue.Enqueue((depth, x + 1, y    ));
				queue.Enqueue((depth, x - 1, y + 1));
				queue.Enqueue((depth, x    , y + 1));
				queue.Enqueue((depth, x + 1, y + 1));
			}
		}

		void FloodFill() {
			"Flood filling".Debug();
			for(int y = 0, i = 0; y < Height; ++y)
				for(var x = 0; x < Width; ++x, ++i)
					if(Pixels[i] != null && !Pixels[i].Flooded)
						Flood(x, y);
		}

		void RemoveNoise() => Patches.RemoveAll(x => x.Count <= 16);

		List<((int, int), (int, int))> TracePatch(List<(int, int)> patch) {
			var pixels = new bool[Width * Height];
			foreach(var (x, y) in patch)
				pixels[y * Width + x] = true;

			bool Check(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height && pixels[y * Width + x];
			void Erase(int x, int y) {
				if(x >= 0 && x < Width && y >= 0 && y < Height) pixels[y * Width + x] = false;
			}
			int Count(int x, int y) {
				var sum = 0;
				for(var i = -1; i <= 1; ++i)
					for(var j = -1; j <= 1; ++j)
						sum += Check(x + i, y + j) ? 1 : 0;
				return sum;
			}
			
			$"Tracing patch of {patch.Count} pixels".Debug();
			
			var lines = new List<((int, int), (int, int))>();
			var queue = new Queue<((int, int), (int, int))>();
			queue.Enqueue((patch[0], (-1, -1)));
			while(queue.Count > 0) {
				var ((x, y), (px, py)) = queue.Dequeue();
				if(Count(x, y) == 0) continue;
				if(px != -1 && py != -1) lines.Add(((px, py), (x, y)));
				Erase(x, y);
				foreach(var (nx, ny) in Neighbors)
					Erase(x + nx, y + ny);

				var opts = new List<(int, int, int)>();
				foreach(var (nx, ny) in Neighbors) {
					var n = Count(x + nx, y + ny);
					if(n != 0) opts.Add((x + nx, y + ny, n));
				}
				foreach(var (nx, ny, _) in opts.OrderByDescending(v => v.Item3))
					queue.Enqueue(((nx, ny), (x, y)));
			}
			
			return lines;
		}

		List<List<(int, int)>> Pathify(List<((int, int), (int, int))> lines) {
			var paths = new List<List<(int, int)>>();
			var mp = new Dictionary<(int, int), List<(int, int)>>();
			foreach(var (a, b) in lines) {
				var path = mp.ContainsKey(a) ? mp[a] : mp.ContainsKey(b) ? mp[b] : null;
				if(path == null) {
					path = new List<(int, int)> { a, b };
					paths.Add(path);
					mp[a] = path;
					mp[b] = path;
					continue;
				}

				var end = path[path.Count - 1];
				if(end == a || end == b) {
					var v = end == a ? b : a;
					path.Add(v);
					mp.Remove(end);
					mp[v] = path;
				} else {
					var start = path[0];
					var v = start == a ? b : a;
					path.Insert(0, v);
					mp.Remove(start);
					mp[v] = path;
				}
			}
			return paths;
		}

		List<List<(int, int)>> Trace() {
			"Tracing patches".Debug();
			var lines = new List<((int, int), (int, int))>();
			foreach(var patch in Patches)
				lines.AddRange(TracePatch(patch));
			"Lines to paths".Debug();
			return Pathify(lines);
		}

		float Dist((int, int) a, (int, int) b) {
			var (c, d) = (b.Item1 - a.Item1, b.Item2 - a.Item2);
			return c * c + d * d;
		}

		List<List<(int, int)>> ReorderPaths(List<List<(int, int)>> paths) {
			"Reordering paths".Debug();
			var last = paths[0].Last();
			var npaths = new List<List<(int, int)>> { paths[0] };
			var remaining = paths.Skip(1).ToList();
			while(remaining.Count != 0) {
				(float, int, bool) closest = (float.PositiveInfinity, -1, false);
				var count = remaining.Count;
				for(var i = 0; i < count; ++i) {
					var elem = remaining[i];
					var sd = Dist(last, elem[0]);
					var ed = Dist(last, elem.Last());
					if(sd <= ed && sd < closest.Item1) {
						closest = (sd, i, false);
						if(sd == 0) break;
					}
					else if(ed < sd && ed < closest.Item1) {
						closest = (ed, i, true);
						if(ed == 0) break;
					}
				}
				var (_, ci, crev) = closest;
				var cpath = remaining[ci];
				remaining.RemoveAt(ci);
				if(crev) cpath.Reverse();
				last = cpath[cpath.Count - 1];
				npaths.Add(cpath);
			}
			return npaths;
		}

		List<List<(int, int)>> JoinPaths(List<List<(int, int)>> paths) {
			"Combining paths".Debug();
			var last = paths[0].Last();
			var npaths = new List<List<(int, int)>> { paths[0] };
			foreach(var path in paths.Skip(1)) {
				var dist = Dist(path[0], last);
				if(dist < 100)
					npaths.Last().AddRange(path.Skip(1));
				else
					npaths.Add(path);
				last = path.Last();
			}
			return npaths;
		}

		List<List<(int, int)>> TriviallyJoinPaths(List<List<(int, int)>> paths) {
			"Trivially combining paths".Debug();
			var mp = new Dictionary<(int, int), List<(int, int)>>();

			foreach(var path in paths) {
				var start = path[0];
				var end = path[path.Count - 1];
				if(mp.ContainsKey(start)) {
					var cpath = mp[start];
					var cstart = cpath[0];
					if(cstart == start) {
						path.Reverse();
						cpath.InsertRange(0, path.SkipLast(1));
						mp.Remove(start);
						mp[end] = cpath;
					} else {
						cpath.AddRange(path.Skip(1));
						mp.Remove(start);
						mp[end] = cpath;
					}
				} else if(mp.ContainsKey(end)) {
					var cpath = mp[end];
					var cstart = cpath[0];
					if(cstart == end) {
						cpath.InsertRange(0, path.SkipLast(1));
						mp.Remove(end);
						mp[start] = cpath;
					} else {
						path.Reverse();
						cpath.AddRange(path.Skip(1));
						mp.Remove(end);
						mp[start] = cpath;
					}
				} else {
					mp[start] = path;
					mp[end] = path;
				}
			}

			var npaths = new List<List<(int, int)>>();
			foreach(var v in mp.Values)
				if(!npaths.Contains(v))
					npaths.Add(v);
			return npaths;
		}

		List<(int, int)> SimplifyPath(List<(int, int)> path) {
			if(path.Count < 3) return path;

			//$"Path elements {path.Count}".Debug();

			var tris = new List<Triangle2D>();
			for(var i = 0; i < path.Count - 2; ++i) {
				var (a, b, c) = (path[i], path[i + 1], path[i + 2]);
				tris.Add(new Triangle2D(
					new Vector2(a.Item1, a.Item2),
					new Vector2(b.Item1, b.Item2),
					new Vector2(c.Item1, c.Item2)
				));
			}

			while(tris.Count > 1) {
				var min = 0;
				var minArea = tris[0].Area;
				for(var i = 1; i < tris.Count; ++i)
					if(tris[i].Area < minArea) {
						min = i;
						minArea = tris[i].Area;
					}
				var minTri = tris[min];
				var extent = (minTri.A - minTri.C).Length();
				if(minArea > extent * 5) break;
				if(min > 0) {
					tris[min - 1].C = tris[min].C;
					tris[min - 1].UpdateArea();
				}
				if(min < tris.Count - 1) {
					tris[min + 1].A = tris[min].A;
					tris[min + 1].UpdateArea();
				}
				tris.RemoveAt(min);
			}

			var npoints = new List<Vector2> { tris[0].A };
			foreach(var tri in tris)
				npoints.Add(tri.B);
			npoints.Add(tris.Last().C);
			
			//$"New path count {npoints.Count}".Debug();

			return npoints.Select(x => ((int) MathF.Round(x.X), (int) MathF.Round(x.Y))).ToList();
		}

		List<List<(int, int)>> SimplifyPaths(List<List<(int, int)>> paths) {
			"Simplifying paths".Debug();
			return paths.Select(SimplifyPath).ToList();
		}

		public void Output(string fn, Page page) {
			"Resizing paths".Debug();
			int lowX = 100000, lowY = 100000;
			int highX = 0, highY = 0;
			foreach(var path in FinalPaths)
				foreach(var (px, py) in path) {
					if(px < lowX) lowX = px;
					if(py < lowY) lowY = py;
					if(px > highX) highX = px;
					if(py > highY) highY = py;
				}
			var off = new Vector2(lowX, lowY);

			var usable = new Vector2(
				page.Width - page.LeftMargin - page.RightMargin, 
				page.Height - page.TopMargin - page.BottomMargin
			);
			var ur = usable.X / usable.Y;
			
			var cur = new Vector2(highX - lowX, highY - lowY);
			var cr = cur.X / cur.Y;
			var nsize = ur > cr ? new Vector2(cur.X * usable.Y / cur.Y, usable.Y) : new Vector2(usable.X, cur.Y * usable.X / cur.X);
			var scale = nsize / cur;
			
			var margin = new Vector2(page.LeftMargin + (usable.X - nsize.X) / 2, page.TopMargin + (usable.Y - nsize.Y) / 2);

			const float fudge = 3.54329f;
			var spaths = FinalPaths.Select(path => path.Select(pe => ((new Vector2(pe.Item1, pe.Item2) - off) * scale + margin) * fudge).ToList());
			
			"Writing SVG".Debug();
			using(var fp = File.Open(fn, FileMode.Create, FileAccess.Write))
				using(var sw = new StreamWriter(fp)) {
					sw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
					sw.WriteLine($"<svg baseProfile=\"tiny\" version=\"1.2\" width=\"{page.Width}mm\" height=\"{page.Height}mm\" viewbox=\"0 0 {page.Width} {page.Height}\" xmlns=\"http://www.w3.org/2000/svg\" xmlns:ev=\"http://www.w3.org/2001/xml-events\" xmlns:xlink=\"http://www.w3.org/1999/xlink\"><defs />");
					foreach(var path in spaths) {
						var f = path[0];
						sw.Write($"<path d=\"M {f.X} {f.Y}");
						foreach(var n in path.Skip(1))
							sw.Write($" L {n.X} {n.Y}");
						sw.WriteLine("\" fill=\"red\" fill-opacity=\"0\" stroke=\"black\" stroke-width=\"1\" />");
					}
					sw.WriteLine("</svg>");
				}
		}
	}
}