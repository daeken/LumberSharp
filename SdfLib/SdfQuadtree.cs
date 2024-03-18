using System.Numerics;
using Common;
using DoubleSharp.MathPlus;

namespace SdfLib;
using static Common.Helpers;
using static MathF;
using static Vector2;
using static Sdf2D;

public class SdfQuadtree {
	const float Epsilon = 0.001f;
	readonly Func<Vector2, float> Scene;
	readonly float MinGridSize;
	readonly Node TopLevel;

	class Node {
		public readonly (Vector2 Min, Vector2 Max) Bounds;
		public readonly bool Filled;
		public readonly Node[] Children;
		public bool? IsSurrounded;
		
		public (Vector2 Min, Vector2 Max) Left => (Bounds.Min, new(Bounds.Min.X, Bounds.Max.Y));
		public (Vector2 Min, Vector2 Max) Right => (new(Bounds.Max.X, Bounds.Min.Y), Bounds.Max);
		public (Vector2 Min, Vector2 Max) Bottom => (Bounds.Min, new(Bounds.Max.X, Bounds.Min.Y));
		public (Vector2 Min, Vector2 Max) Top => (new(Bounds.Min.X, Bounds.Max.Y), Bounds.Max);

		public Node((Vector2 Min, Vector2 Max) bounds) {
			Bounds = bounds;
			Filled = true;
			Children = null;
		}

		public Node((Vector2 Min, Vector2 Max) bounds, Node[] children) {
			Bounds = bounds;
			Children = children;
		}
	}

	public SdfQuadtree(Func<Vector2, float> scene, float minGridSize = 0.01f) {
		Scene = scene;
		MinGridSize = minGridSize;

		var (origin, radius) = FindBoundingCircle(scene);
		radius *= 1.1f;
		var (min, max) = (origin - new Vector2(radius), origin + new Vector2(radius));
		var size = (max - min).Length(); // sqrt(r^2 + r^2) but I'm lazy

		TopLevel = Subdivide(min, max, size);
	}

	Node Subdivide(Vector2 min, Vector2 max, float size) {
		var center = Mix(min, max, 0.5f);
		if(size <= MinGridSize)
			return Scene(center) <= MinGridSize ? new Node((min, max)) : null;

		var hs = size / 2;
		var a = Subdivide(min, center, hs);
		var b = Subdivide(new(center.X, min.Y), new(max.X, center.Y), hs);
		var c = Subdivide(new(min.X, center.Y), new(center.X, max.Y), hs);
		var d = Subdivide(center, max, hs);

		if(a == null && b == null && c == null && d == null) return null;
		if((a?.Filled ?? false) && (b?.Filled ?? false) &&
		   (c?.Filled ?? false) && (d?.Filled ?? false))
			return new((min, max));

		return new((min, max), new[] { a, b, c, d });
	}

	public List<List<Vector2>> FindPaths() {
		var paths = new List<List<Vector2>>();
		foreach(var node in FindFilled(TopLevel)) {
			if(IsSurrounded(node)) continue;
			var spaths = PathsForNode(node);
			paths.AddRange(spaths);
		}
		paths = paths.ReorderPaths().TriviallyJoinPaths();
		//paths = paths.ReorderPaths().JoinPaths(0.01f);
		paths = paths.Subdivide(10);
		paths = paths.Apply(p => FindClosestSurfacePoint(Scene, p));
		paths = paths.SimplifyPaths(0.001f);
		paths = paths.ReorderPaths().TriviallyJoinPaths();
		Console.WriteLine($"Found {paths.Count} paths");
		return paths;
	}

	IEnumerable<List<Vector2>> PathsForSide(
		Node node,
		Func<Node, (Vector2 Min, Vector2 Max)> side,
		Func<Node, (Vector2 Min, Vector2 Max)> opposite,
		Func<Vector2, float> axis,
		Func<Vector2, float, Vector2> fromAxis
	) {
		var nside = side(node);
		var segments = new List<(float Min, float Max)> { (axis(nside.Min), axis(nside.Max)) };
		var touching = FindTouching(nside, except: node).Select(opposite).ToList();
		foreach(var other in touching) {
			var (omin, omax) = (axis(other.Min), axis(other.Max));
			for(var i = 0; i < segments.Count; ++i) {
				var segment = segments[i];
				var (smin, smax) = segment;
				// Completely obscured
				if(omin <= smin && smax <= omax) {
					segments.RemoveAt(i);
					i--;
					continue;
				}
				// Trim beginning
				if(omin <= smin && omax < smax)
					segments[i] = (omax, segment.Max);
				// Trim end
				else if(smin < omin && smax <= omax)
					segments[i] = (segment.Min, omin);
				// Remove middle
				else if(smin < omin && omax < smax) {
					segments[i] = (segment.Min, omin);
					segments.Add((omax, segment.Max));
				}
			}
		}
		return segments.Select(x => new List<Vector2> { fromAxis(nside.Min, x.Min), fromAxis(nside.Min, x.Max) });
	}

	List<List<Vector2>> PathsForNode(Node node) {
		var leftPaths = PathsForSide(
			node, 
			x => x.Left, x => x.Right, 
			x => x.Y, (v, x) => new(v.X, x)
		);
		var rightPaths = PathsForSide(
			node, 
			x => x.Right, x => x.Left, 
			x => x.Y, (v, x) => new(v.X, x)
		);
		var bottomPaths = PathsForSide(
			node, 
			x => x.Bottom, x => x.Top, 
			x => x.X, (v, x) => new(x, v.Y)
		);
		var topPaths = PathsForSide(
			node, 
			x => x.Top, x => x.Bottom, 
			x => x.X, (v, x) => new(x, v.Y)
		);
		return leftPaths.Concat(rightPaths)
			.Concat(bottomPaths).Concat(topPaths).ToList()
			.TriviallyJoinPaths();
	}

	bool IsCovered(
		Node node, 
		Func<Node, (Vector2 Min, Vector2 Max)> side,
		Func<Node, (Vector2 Min, Vector2 Max)> opposite, 
		Func<Vector2, float> axis
	) {
		var nside = side(node);
		var nmin = axis(nside.Min);
		var nmax = axis(nside.Max);
		var touching = FindTouching(nside, except: node).Select(opposite).ToList();
		if(touching.Count == 0) return false;
		var min = touching.Min(x => axis(x.Min));
		var max = touching.Max(x => axis(x.Max));
		return min <= nmin && nmax <= max;
	}

	bool IsSurrounded(Node node) {
		if(node.IsSurrounded != null) return node.IsSurrounded.Value;
		node.IsSurrounded =
			IsCovered(node, x => x.Left, x => x.Right, x => x.Y) &&
			IsCovered(node, x => x.Right, x => x.Left, x => x.Y) &&
			IsCovered(node, x => x.Bottom, x => x.Top, x => x.X) &&
			IsCovered(node, x => x.Top, x => x.Bottom, x => x.X);
		return node.IsSurrounded.Value;
	}

	IEnumerable<Node> FindFilled(Node node) {
		if(node == null) yield break;
		if(node.Filled)
			yield return node;
		else
			foreach(var child in node.Children)
				foreach(var res in FindFilled(child))
					yield return res;
	}

	IEnumerable<Node> FindTouching((Vector2 Min, Vector2 Max) line, Node except) =>
		FindTouching(line).Where(x => x != except);

	IEnumerable<Node> FindTouching((Vector2 Min, Vector2 Max) line) => 
		line.Min.X == line.Max.X
			? FindTouchingX(line.Min.X, line.Min.Y, line.Max.Y, TopLevel)
			: FindTouchingY(line.Min.Y, line.Min.X, line.Max.X, TopLevel);

	bool Overlapping(float a, float b, float c, float d) =>
		(a <= c && c < b) || 
		(a < d && d <= b) ||
		(c <= a && a < d) ||
		(c < b && b <= d);
	
	IEnumerable<Node> FindTouchingX(float x, float minY, float maxY, Node node) {
		if(node == null) yield break;
		if(node.Bounds.Min.X > x || x > node.Bounds.Max.X) yield break;
		if(node.Filled) {
			var left = node.Left;
			var right = node.Right;
			if((left.Min.X == x && Overlapping(minY, maxY, left.Min.Y, left.Max.Y)) ||
			   (right.Min.X == x && Overlapping(minY, maxY, right.Min.Y, right.Max.Y)))
				yield return node;
		} else
			foreach(var child in node.Children)
				foreach(var res in FindTouchingX(x, minY, maxY, child))
					yield return res;
	}

	IEnumerable<Node> FindTouchingY(float y, float minX, float maxX, Node node) {
		if(node == null) yield break;
		if(node.Bounds.Min.Y > y || y > node.Bounds.Max.Y) yield break;
		if(node.Filled) {
			var bottom = node.Bottom;
			var top = node.Top;
			if((bottom.Min.Y == y && Overlapping(minX, maxX, bottom.Min.X, bottom.Max.X)) ||
			   (top.Min.Y == y && Overlapping(minX, maxX, top.Min.X, top.Max.X)))
				yield return node;
		} else
			foreach(var child in node.Children)
				foreach(var res in FindTouchingY(y, minX, maxX, child))
					yield return res;
	}
}