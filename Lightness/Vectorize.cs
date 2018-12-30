using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Common;
using Lightness.Renderer;
using PrettyPrinter;
using static Common.SvgHelper;

namespace Lightness {
	public class Vectorize {
		readonly Pixel[] Pixels;
		readonly int Width, Height;
		
		readonly List<List<Vector2>> FinalPaths;
		
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
			paths = SimplifyPaths(paths, 5);
			paths = ReorderPaths(paths);
			paths = JoinPaths(paths);
			paths = SimplifyPaths(paths, 5);
			paths = ReorderPaths(paths);
			FinalPaths = paths;
		}

		public void Output(string fn, Page page) =>
			SvgHelper.Output(fn, FinalPaths, page);

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

		List<(Vector2, Vector2)> TracePatch(List<(int, int)> patch) {
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
			
			var lines = new List<(Vector2, Vector2)>();
			var queue = new Queue<((int, int), (int, int))>();
			queue.Enqueue((patch[0], (-1, -1)));
			while(queue.Count > 0) {
				var ((x, y), (px, py)) = queue.Dequeue();
				if(Count(x, y) == 0) continue;
				if(px != -1 && py != -1) lines.Add((new Vector2(px, py), new Vector2(x, y)));
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

		List<List<Vector2>> Pathify(List<(Vector2, Vector2)> lines) {
			var paths = new List<List<Vector2>>();
			var mp = new Dictionary<Vector2, List<Vector2>>();
			foreach(var (a, b) in lines) {
				var path = mp.ContainsKey(a) ? mp[a] : mp.ContainsKey(b) ? mp[b] : null;
				if(path == null) {
					path = new List<Vector2> { a, b };
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

		List<List<Vector2>> Trace() {
			"Tracing patches".Debug();
			var lines = new List<(Vector2, Vector2)>();
			foreach(var patch in Patches)
				lines.AddRange(TracePatch(patch));
			"Lines to paths".Debug();
			return Pathify(lines);
		}
	}
}