using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Xml;
using SvgNet;
using SvgNet.Elements;
using SvgNet.Types;

namespace Common {
	public static class SvgHelper {
		public static List<List<Vector2>> Apply(this List<List<Vector2>> paths, Func<Vector2, Vector2> func) =>
			paths.Select(path => path.Select(func).ToList()).ToList();
		public static List<List<Vector3>> Apply(this List<List<Vector2>> paths, Func<Vector2, Vector3> func) =>
			paths.Select(path => path.Select(func).ToList()).ToList();
		public static List<List<Vector2>> Apply(this List<List<Vector3>> paths, Func<Vector3, Vector2> func) =>
			paths.Select(path => path.Select(func).ToList()).ToList();
		public static List<(string Color, List<Vector2> Path)> Apply(this List<(string Color, List<Vector2> Path)> paths, Func<Vector2, Vector2> func) =>
			paths.Select(path => (path.Color, path.Path.Select(func).ToList())).ToList();
		public static List<Vector2> Apply(this List<Vector2> path, Func<Vector2, Vector2> func) =>
			path.Select(func).ToList();

		public static List<Vector2> Transform(this List<Vector2> path, Matrix4x4 transform) =>
			path.Apply(p => Vector2.Transform(p, transform));

		public static List<List<Vector2>> Transform(this List<List<Vector2>> paths, Matrix4x4 transform) =>
			paths.Apply(p => Vector2.Transform(p, transform));

		public static List<(string Color, List<Vector2> Path)> ByColor(
			this List<(string Color, List<Vector2> Path)> paths,
			Func<List<List<Vector2>>, IEnumerable<IEnumerable<Vector2>>> func
		) =>
			paths.GroupBy(x => x.Color)
				.Select(x => func(x.Select(y => y.Path).ToList()).Select(y => (x.Key, y.ToList())))
				.SelectMany(x => x)
				.ToList();

		public static List<List<Vector2>> SegmentsToPaths(this List<(Vector2, Vector2)> lines) {
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
		
		public static List<List<Vector2>> ReorderPaths(this List<List<Vector2>> paths) {
			if(paths.Count <= 1) return paths;
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

		public static List<Vector2> SimplifyPath(this List<Vector2> path, float minAreaRatio) {
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

		public static List<List<Vector2>> MakeUniformDistances(this List<List<Vector2>> paths) {
			var minDist = paths.Select(x =>
					x.SkipLast(1).Zip(x.Skip(1)).Select(y => (y.First - y.Second).Length()).Min())
				.Min();
			return paths.Select(path =>
				path.SkipLast(1).Zip(path.Skip(1)).Select(x => {
					var (a, b) = (x.First, x.Second);
					var dist = (a - b).Length();
					var chunks = (int) MathF.Round(dist / minDist);
					var points = new List<Vector2>();
					var step = (b - a) / chunks;
					for(var i = 0; i < chunks; ++i)
						points.Add(a + step * i);
					return points;
				}).SelectMany(x => x).Append(path.Last()).ToList()).ToList();
		}

		public static List<List<Vector2>> Subdivide(this List<List<Vector2>> paths, int count) => 
			paths.Select(path =>
				path.SkipLast(1).Zip(path.Skip(1)).Select(x => {
					var (a, b) = (x.First, x.Second);
					var dist = (a - b).Length();
					var points = new List<Vector2>();
					var step = (b - a) / count;
					for(var i = 0; i < count; ++i)
						points.Add(a + step * i);
					return points;
				}).SelectMany(x => x).Append(path.Last()).ToList()).ToList();

		public static List<List<Vector2>> RemoveOverlaps(this List<List<Vector2>> paths) {
			var quadtree = new QuadTree(paths);
			quadtree.RemoveOverlaps();
			return quadtree.GetPaths();
		}
		
		public static List<List<Vector2>> ScalePaths(this List<List<Vector2>> paths, float scale) =>
			paths.Select(path => path.Select(p => p * scale).ToList()).ToList();
		
		public static List<List<Vector2>> SimplifyPaths(this List<List<Vector2>> paths, float minAreaRatio) =>
			paths
				//.AsParallel()
				.Select(x => SimplifyPath(x, minAreaRatio)).ToList();
		
		public static List<List<Vector2>> JoinPaths(this List<List<Vector2>> paths, float minDist = 100) {
			if(paths.Count < 2) return paths;
			var last = paths[0].Last();
			var npaths = new List<List<Vector2>> { paths[0] };
			foreach(var path in paths.Skip(1)) {
				var dist = (path[0] - last).LengthSquared();
				if(dist < minDist)
					npaths.Last().AddRange(path.Skip(1));
				else
					npaths.Add(path);
				last = path.Last();
			}
			return npaths;
		}

		public static List<List<Vector2>> TriviallyJoinPaths(this List<List<Vector2>> paths) {
			var mp = new Dictionary<Vector2, List<Vector2>>();

			foreach(var path in paths) {
				var start = path[0];
				var end = path[^1];
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

		public static (Vector2 Lower, Vector2 Upper) GetBounds(this List<List<Vector2>> paths) {
			var minX = float.PositiveInfinity;
			var minY = float.PositiveInfinity;
			var maxX = float.NegativeInfinity;
			var maxY = float.NegativeInfinity;
			foreach(var path in paths)
				foreach(var point in path) {
					if(point.X < minX) minX = point.X;
					if(point.X > maxX) maxX = point.X;
					if(point.Y < minY) minY = point.Y;
					if(point.Y > maxY) maxY = point.Y;
				}

			return (new(minX, minY), new(maxX, maxY));
		}

		public static float CalcDrawDistance(this List<List<Vector2>> paths) =>
			paths.Select(CalcDrawDistance).Sum();
		public static float CalcDrawDistance(this List<Vector2> path) =>
			path.SkipLast(1).Zip(path.Skip(1)).Select(x => (x.Second - x.First).Length()).Sum();

		public static List<List<Vector2>> Fit(this List<List<Vector2>> paths, Vector2 size) {
			var (offset, ub) = GetBounds(paths);

			var osize = ub - offset;
			Console.WriteLine($"{osize.X} {osize.Y} vs {size.X} {size.Y}");
			if(osize.X <= size.X && osize.Y <= size.Y) return paths;

			var scale = (osize.X >= osize.Y) ? size.X / osize.X : size.Y / osize.Y;
			var push = new Vector2(
				osize.X * scale < size.X ? (size.X - osize.X * scale) / 2 : 0, 
				osize.Y * scale < size.Y ? (size.Y - osize.Y * scale) / 2 : 0
			);

			return paths.Select(path => path.Select(x => (x - offset) * scale + push).ToList()).ToList();
		}

		public static List<(string Color, List<Vector2> Points)> OldPathsFromSvg(string fn) {
			var xml = new XmlDocument();
			xml.LoadXml(File.ReadAllText(fn));
			var svg = SvgFactory.LoadFromXML(xml, null);
			var paths = new List<(string Color, List<Vector2> Points)>();
			foreach(var elem in svg.Children)
				switch(elem) {
					case SvgElement element:
						paths.AddRange(PathsFromElement(element, Matrix4x4.Identity));
						break;
					default:
						throw new NotImplementedException($"Foo? {elem}");
				}
			return paths;
		}

		static IEnumerable<(string Color, List<Vector2> Points)> PathsFromElement(SvgElement element, Matrix4x4 transform) {
			var curTransform = transform;
			if(element is SvgStyledTransformedElement ste)
				curTransform = FlattenTransforms(ste.Transform, transform);

			Vector2 T(Vector2 p) {
				var tp = Vector4.Transform(p, curTransform);
				return new Vector2(tp.X, tp.Y);
			}
			
			switch(element) {
				case SvgPathElement pe:
					var de = pe["d"] switch { SvgPath sp => sp, string s => new SvgPath(s), _ => throw new NotSupportedException() };
					var pos = Vector2.Zero;
					var lines = new List<(Vector2, Vector2)>();
					var first = Vector2.Zero;
					for(var i = 0; i < de.Count; ++i) {
						var ps = de[i];
						switch(ps.Type) {
							case SvgPathSegType.SVG_SEGTYPE_MOVETO:
								pos = (ps.Abs ? Vector2.Zero : pos) + new Vector2(ps.Data[0], ps.Data[1]);
								break;
							case SvgPathSegType.SVG_SEGTYPE_LINETO:
								var tgt = (ps.Abs ? Vector2.Zero : pos) + new Vector2(ps.Data[0], ps.Data[1]);
								lines.Add((pos, tgt));
								pos = tgt;
								break;
							case SvgPathSegType.SVG_SEGTYPE_VLINETO:
								var vtgt = ps.Abs ? new Vector2(pos.X, ps.Data[0]) : new Vector2(pos.X, pos.Y + ps.Data[0]);
								lines.Add((pos, vtgt));
								pos = vtgt;
								break;
							case SvgPathSegType.SVG_SEGTYPE_HLINETO:
								var htgt = ps.Abs ? new Vector2(ps.Data[0], pos.Y) : new Vector2(pos.X + ps.Data[0], pos.Y);
								lines.Add((pos, htgt));
								pos = htgt;
								break;
							case SvgPathSegType.SVG_SEGTYPE_CURVETO:
								var c1 = (ps.Abs ? Vector2.Zero : pos) + new Vector2(ps.Data[0], ps.Data[1]);
								var c2 = (ps.Abs ? Vector2.Zero : pos) + new Vector2(ps.Data[2], ps.Data[3]);
								var end = (ps.Abs ? Vector2.Zero : pos) + new Vector2(ps.Data[4], ps.Data[5]);
								var points = CubicBezierToLines(pos, c1, c2, end).ToList();
								lines.AddRange(points.SkipLast(1).Zip(points.Skip(1)));
								pos = end;
								break;
							case SvgPathSegType.SVG_SEGTYPE_CLOSEPATH:
								lines.Add((pos, first));
								pos = first;
								break;
							default:
								throw new NotImplementedException(ps.Type.ToString());
						}
						if(i == 0) first = pos;
					}

					var slines = lines.Select(x => new List<Vector2> { x.Item1, x.Item2 }).ToList();
					foreach(var path in SimplifyPaths(TriviallyJoinPaths(slines), .5f))
						yield return (GetColor(pe), path.Select(T).ToList());
					break;
			}
			foreach(var elem in element.Children)
				if(elem is SvgElement sub)
					foreach(var path in PathsFromElement(sub, curTransform))
						yield return path;
		}

		static string GetColor(SvgStyledTransformedElement ste) => (string) ste.Style["stroke"];

		static IEnumerable<Vector2> CubicBezierToLines(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3) {
			for(var t = 0f; t < 1; t += 0.001f) {
				var it3 = MathF.Pow(1 - t, 3);
				var it2 = MathF.Pow(1 - t, 2);
				var t3 = MathF.Pow(t, 3);
				var t2 = MathF.Pow(t, 2);
				yield return p0 * it3 + p1 * 3 * t * it2 + p2 * 3 * t2 * (1 - t) + p3 * t3;
			}
			yield return p3;
		}

		static Matrix4x4 FlattenTransforms(SvgTransformList stl, Matrix4x4 mat) {
			return mat;
		}
		
		static Matrix4x4 ConvTransform(Matrix m) => Matrix4x4.Identity;

		public static void Output(string fn, List<List<Vector2>> paths, Page page, bool addBounds = false) =>
			Output(fn, paths.Select(x => ("black", x)).ToList(), page, addBounds);
		
		public static void Output(string fn, List<(string Color, List<Vector2> Points)> paths, Page page, bool addBounds = false) {
			float lowX = 100000f, lowY = 100000f;
			float highX = 0f, highY = 0f;
			foreach(var (color, path) in paths)
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
			var spaths = paths.GroupBy(x => x.Color)
				.Select(x => (x.Key,
					x.Select(y => y.Points.Select(pe => ((pe - off) * scale + margin) * fudge).ToList()).ToList()))
				.ToDictionary(x => x.Key, x => x.Item2);

			lowX = 100000f;
			lowY = 100000f;
			highX = 0f;
			highY = 0f;
			foreach(var cpaths in spaths.Values)
				foreach(var path in cpaths)
					foreach(var p in path) {
						if(p.X < lowX) lowX = p.X;
						if(p.Y < lowY) lowY = p.Y;
						if(p.X > highX) highX = p.X;
						if(p.Y > highY) highY = p.Y;
					}

			using var fp = File.Open(fn, FileMode.Create, FileAccess.Write);
			using var sw = new StreamWriter(fp);
			sw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
			sw.WriteLine($"<svg baseProfile=\"tiny\" version=\"1.2\" width=\"{page.Width}mm\" height=\"{page.Height}mm\" viewbox=\"0 0 {page.Width} {page.Height}\" xmlns=\"http://www.w3.org/2000/svg\" xmlns:ev=\"http://www.w3.org/2001/xml-events\" xmlns:xlink=\"http://www.w3.org/1999/xlink\"><defs />");
			foreach(var sspaths in spaths.GroupBy(x => x.Key.Contains("###") ? x.Key.Split("###", 2)[1] : "")) {
				sw.WriteLine("<g>");
				foreach(var (color, cpaths) in sspaths.OrderBy(x => x.Key.Split("###")[0] == "black")) {
					sw.Write("<path d=\"");
					foreach(var path in cpaths) {
						var f = path[0];
						sw.Write($"M {f.X} {f.Y} ");
						foreach(var n in path.Skip(1))
							sw.Write($"L {n.X} {n.Y} ");
					}
					sw.WriteLine(
						$"\" fill=\"rgba(255,255,255,0.5)\" fill-opacity=\"0\" stroke=\"{color.Split("###")[0]}\" stroke-width=\"1\" />");
				}
				sw.WriteLine("</g>");
			}

			if(addBounds) {
				var cutMargin = 25;
				sw.Write("<path d=\"");
				sw.Write($"M {lowX + cutMargin} {lowY + cutMargin} ");
				sw.Write($"L {highX - cutMargin} {lowY + cutMargin} ");
				sw.Write($"L {highX - cutMargin} {highY - cutMargin} ");
				sw.Write($"L {lowX + cutMargin} {highY - cutMargin}");
				sw.Write($"L {lowX + cutMargin} {lowY - cutMargin}");
				sw.WriteLine("\" stroke=\"black\" fill-opacity=\"0\" stroke-width=\"1\" />");
			}

			sw.WriteLine("</svg>");
		}
	}
}