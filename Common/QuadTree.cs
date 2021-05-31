using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using static Common.Helpers;

namespace Common {
	public class QuadTree {
		int MaxSegmentsPerNode;
		
		public List<(Vector2 A, Vector2 B)> Segments;
		public QuadTree[] Children;
		
		public QuadTree(List<Vector2> path, int maxSegmentsPerNode) : this(new List<List<Vector2>> { path }, maxSegmentsPerNode) {}

		public QuadTree(List<List<Vector2>> paths, int maxSegmentsPerNode = 50) : this(
			paths.Select(path => path.SkipLast(1).Zip(path.Skip(1)).Select(OrderSegment)).SelectMany(x => x).ToList(),
			SvgHelper.GetBounds(paths), maxSegmentsPerNode) {}

		public QuadTree(List<(Vector2 A, Vector2 B)> segments, (Vector2 Lower, Vector2 Upper) bounds, int maxSegmentsPerNode) {
			MaxSegmentsPerNode = maxSegmentsPerNode;
			if(segments.Count <= MaxSegmentsPerNode) {
				Segments = CleanSegments(segments);
				return;
			}

			var (lb, ub) = bounds;
			var center = Mix(lb, ub, 0.5f);

			Children = new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) }.Select(c => {
				var (x, y) = c;
				var nb = (
					new Vector2(x < 0 ? lb.X : center.X, y < 0 ? lb.Y : center.Y), 
					new Vector2(x < 0 ? center.X : ub.X, y < 0 ? center.Y : ub.Y)
				);
				return new QuadTree(
					segments.Select(x => GetWithin(x, nb)).SelectMany(x => x).ToList(), 
					nb, maxSegmentsPerNode
				);
			}).ToArray();
		}

		List<(Vector2 A, Vector2 B)> CleanSegments(List<(Vector2 A, Vector2 B)> segments) {
			return segments.Select(OrderSegment).GroupBy(v => MathF.Round((v.B - v.A).Apply(MathF.Atan2), 1)).Select(
				segs => {
					var points = segs.Select(v => new[] { v.A, v.B }).SelectMany(x => x).OrderBy(v => v.X)
						.ThenBy(v => v.Y).ToList();
					var (first, second, tlen) = points.Select(a => points.Select(b => (a, b, (b - a).Length()))).SelectMany(x => x)
						.OrderByDescending(x => x.Item3).First();
					return OrderSegment((first, second));
				}).ToList();
		}
		
		[Flags]
		enum OutCode {
			Inside = 0, 
			Left = 1, 
			Right = 2, 
			Bottom = 4, 
			Top = 8
		}

		IEnumerable<(Vector2 A, Vector2 B)> GetWithin((Vector2 A, Vector2 B) segment, (Vector2 Lower, Vector2 Upper) bounds) {
			var (a, b) = segment;
			var (lb, ub) = bounds;

			OutCode GetOutCode(Vector2 p) {
				var code = OutCode.Inside;
				if(p.X < lb.X) code |= OutCode.Left;
				if(p.X > ub.X) code |= OutCode.Right;
				if(p.Y < lb.Y) code |= OutCode.Top;
				if(p.Y > ub.Y) code |= OutCode.Bottom;
				return code;
			}

			Vector2 CalcIntersection(OutCode clipTo) {
				var d = b - a;
				var sy = d.X / d.Y;
				var sx = d.Y / d.X;
				
				if(clipTo.HasFlag(OutCode.Top)) return new(a.X + sy * (lb.Y - a.Y), lb.Y);
				if(clipTo.HasFlag(OutCode.Bottom)) return new(a.X + sy * (ub.Y - a.Y), ub.Y);
				if(clipTo.HasFlag(OutCode.Right)) return new(ub.X, a.Y + sx * (ub.X - a.X));
				if(clipTo.HasFlag(OutCode.Left)) return new(lb.X, a.Y + sx * (lb.X - a.X));
				throw new NotSupportedException();
			}

			var oc1 = GetOutCode(a);
			var oc2 = GetOutCode(b);
			var accept = false;

			while(true) {
				if((oc1 | oc2) == OutCode.Inside) {
					accept = true;
					break;
				}

				if((oc1 & oc2) != 0) break;

				var oc = oc1 != OutCode.Inside ? oc1 : oc2;
				var p = CalcIntersection(oc);
				if(oc == oc1)
					oc1 = GetOutCode(a = p);
				else
					oc2 = GetOutCode(b = p);
			}

			if(accept) yield return OrderSegment((a, b));
		}

		static (Vector2 A, Vector2 B) OrderSegment((Vector2 A, Vector2 B) segment) {
			var (a, b) = segment;
			if(a.X < b.X) return segment;
			if(a.X > b.X || a.Y > b.Y) return (b, a);
			return segment;
		}

		public List<List<Vector2>> GetPaths() {
			var paths = Segments != null
				? Segments.Select(x => new List<Vector2> { x.A, x.B }).ToList()
				: Children.Select(x => x.GetPaths()).SelectMany(x => x).ToList();

			if(paths.Count == 0) return paths;

			paths = SvgHelper.ReorderPaths(paths);
			paths = SvgHelper.TriviallyJoinPaths(paths);
			paths = SvgHelper.ReorderPaths(paths);
			
			return paths;
		}
	}
}