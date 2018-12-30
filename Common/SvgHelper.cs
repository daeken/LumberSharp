using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Common {
	public static class SvgHelper {
		public static List<List<Vector2>> ReorderPaths(List<List<Vector2>> paths) {
			var last = paths[0].Last();
			var npaths = new List<List<Vector2>> { paths[0] };
			var remaining = paths.Skip(1).ToList();
			while(remaining.Count != 0) {
				(float, int, bool) closest = (float.PositiveInfinity, -1, false);
				var count = remaining.Count;
				for(var i = 0; i < count; ++i) {
					var elem = remaining[i];
					var sd = (last - elem[0]).Length();
					var ed = (last - elem.Last()).Length();
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
		
		static List<Vector2> SimplifyPath(List<Vector2> path, float minAreaRatio) {
			if(path.Count < 3) return path;

			var tris = new List<Triangle2D>();
			for(var i = 0; i < path.Count - 2; ++i) {
				var (a, b, c) = (path[i], path[i + 1], path[i + 2]);
				tris.Add(new Triangle2D(a, b, c));
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
				if(minArea > extent * minAreaRatio) break;
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
			
			return npoints;
		}
		
		public static List<List<Vector2>> SimplifyPaths(List<List<Vector2>> paths, float minAreaRatio) =>
			paths.Select(x => SimplifyPath(x, minAreaRatio)).ToList();
		
		public static List<List<Vector2>> JoinPaths(List<List<Vector2>> paths) {
			var last = paths[0].Last();
			var npaths = new List<List<Vector2>> { paths[0] };
			foreach(var path in paths.Skip(1)) {
				var dist = (path[0] - last).LengthSquared();
				if(dist < 100)
					npaths.Last().AddRange(path.Skip(1));
				else
					npaths.Add(path);
				last = path.Last();
			}
			return npaths;
		}

		public static List<List<Vector2>> TriviallyJoinPaths(List<List<Vector2>> paths) {
			var mp = new Dictionary<Vector2, List<Vector2>>();

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

			var npaths = new List<List<Vector2>>();
			foreach(var v in mp.Values)
				if(!npaths.Contains(v))
					npaths.Add(v);
			return npaths;
		}
		
		public static void Output(string fn, List<List<Vector2>> paths, Page page) {
			float lowX = 100000f, lowY = 100000f;
			float highX = 0f, highY = 0f;
			foreach(var path in paths)
				foreach(var p in path) {
					if(p.X < lowX) lowX = p.X;
					if(p.Y < lowY) lowY = p.Y;
					if(p.X > highX) highX = p.X;
					if(p.Y > highY) highY = p.Y;
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
			var spaths = paths.Select(path => path.Select(pe => ((pe - off) * scale + margin) * fudge).ToList());
			
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