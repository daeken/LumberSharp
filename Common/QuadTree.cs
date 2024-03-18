using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using DoubleSharp.Linq;
using static Common.Helpers;

namespace Common {
	public class QuadTree {
		int MaxSegmentsPerNode;
		
		public List<(Vector2 A, Vector2 B)> Segments;
		public QuadTree[] Children;
		
		public QuadTree(List<Vector2> path, int maxSegmentsPerNode) : this(new List<List<Vector2>> { path }, maxSegmentsPerNode) {}

		public QuadTree(List<List<Vector2>> paths, int maxSegmentsPerNode = 50) : this(
			paths.Select(path => path.SkipLast(1).Zip(path.Skip(1)).Select(OrderSegment)).SelectMany(x => x).ToList(),
			paths.GetBounds(), maxSegmentsPerNode) {}

		public QuadTree(List<(Vector2 A, Vector2 B)> segments, (Vector2 Lower, Vector2 Upper) bounds, int maxSegmentsPerNode) {
			MaxSegmentsPerNode = maxSegmentsPerNode;
			if(segments.Count <= MaxSegmentsPerNode) {
				Segments = segments;
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

			return paths.ReorderPaths()
				.TriviallyJoinPaths()
				.ReorderPaths();
		}

		public void RemoveOverlaps() {
			if(Segments == null) {
				Children.ForEach(x => x.RemoveOverlaps());
				return;
			}

			var nsegments = new List<(Vector2 A, Vector2 B)>();
			foreach(var segment in Segments) {
				var didOverlap = false;
				for(var i = 0; i < nsegments.Count; ++i) {
					var nseg = nsegments[i];
					if((nseg.A == segment.A && nseg.B == segment.B) ||
					   (nseg.A == segment.B && nseg.B == segment.A)) {
						didOverlap = true;
						break;
					}
					if(DetermineOverlap(segment, nseg)) {
						nsegments[i] = CombineSegments(segment, nseg);
						didOverlap = true;
						break;
					}
				}
				if(didOverlap) continue;
				nsegments.Add(segment);
			}
			Segments = nsegments;
		}

		static bool IsCollinear(Vector2 a, Vector2 b, Vector2 c) =>
			MathF.Abs((b.Y - a.Y) * (c.X - b.X) - (b.X - a.X) * (c.Y - b.Y)) < 0.0001f;

		static bool OnSegment(Vector2 p, Vector2 q, Vector2 r) =>
			q.X <= MathF.Max(p.X, r.X) && q.X >= MathF.Min(p.X, r.X) &&
			q.Y <= MathF.Max(p.Y, r.Y) && q.Y >= MathF.Min(p.Y, r.Y);

		static bool DetermineOverlap(
			(Vector2 Start, Vector2 End) a, 
			(Vector2 Start, Vector2 End) b
		) =>
			IsCollinear(a.Start, a.End, b.Start) && IsCollinear(a.Start, a.End, b.End) &&
			(OnSegment(a.Start, b.Start, a.End) || OnSegment(a.Start, b.End, a.End));

		static (Vector2 Start, Vector2 End) CombineSegments(
			(Vector2 Start, Vector2 End) a,
			(Vector2 Start, Vector2 End) b
		) {
			var points = new List<Vector2> { a.Start, a.End, b.Start, b.End };
			points.Sort((v1, v2) => v1.X == v2.X ? v1.Y.CompareTo(v2.Y) : v1.X.CompareTo(v2.X));
			return (points[0], points[3]);
		}
	}
}