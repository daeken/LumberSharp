using System.Numerics;
using DoubleSharp.MathPlus;

namespace Facer;

public class Triangle3D(Vector3 a, Vector3 b, Vector3 c) {
	public readonly Vector3 A = a, B = b, C = c;
	public readonly Vector3 Normal = CalcNormal(a, b, c);
	public readonly Vector3 Centroid = (a + b + c) / 3;

	public Triangle3D SwapYZ() =>
		new(
			new(A.X, -A.Z, A.Y),
			new(B.X, -B.Z, B.Y),
			new(C.X, -C.Z, C.Y)
		);

	static Vector3 CalcNormal(Vector3 a, Vector3 b, Vector3 c) {
		var u = b - a;
		var v = c - a;
		return new Vector3(
			u.Y * v.Z - u.Z * v.Y,
			u.Z * v.X - u.X * v.Z,
			u.X * v.Y - u.Y * v.X
		).Normalize();
	}

	public IEnumerable<Vector3> Points {
		get {
			yield return A;
			yield return B;
			yield return C;
		}
	}

	// Moller-Trumbore
	public (Vector3, float)? FindIntersection(Vector3 origin, Vector3 direction) {
		var edge1 = B - A;
		var edge2 = C - A;
		var h = Vector3.Cross(direction, edge2);
		var a = Vector3.Dot(edge1, h);
		if(MathF.Abs(a) < 0.001f) return null; // If it's < Epsilon but > -Epsilon, this is a miss; < -Epsilon means hitting a triangle on the opposite face
		var f = 1 / a;
		var s = origin - A;
		var u = f * Vector3.Dot(s, h);
		if(u < 0 || u > 1) return null;
		var q = Vector3.Cross(s, edge1);
		var v = f * Vector3.Dot(direction, q);
		if(v < 0 || u + v > 1) return null;
		var t = f * Vector3.Dot(edge2, q);
		if(t <= 0.001f) return null;
		return (origin + direction * t, t);
	}

	public bool Intersects(Triangle3D b) =>
		IntersectEdgeTriangle(A, B, b) || 
		IntersectEdgeTriangle(B, C, b) || 
		IntersectEdgeTriangle(C, A, b) || 
		IntersectEdgeTriangle(b.A, b.B, this) || 
		IntersectEdgeTriangle(b.B, b.C, this) || 
		IntersectEdgeTriangle(b.C, b.A, this);

	bool IntersectEdgeTriangle(Vector3 a, Vector3 b, Triangle3D tri) =>
		FindIntersection(a, (b - a).Normalize()) != null;

	public Triangle3D Transform(Func<Vector3, Vector3> func) => new(func(A), func(B), func(C));

	public IEnumerable<Triangle3D> Split(Vector3 normal, float distance) {
		var d1 = Vector3.Dot(A, normal) - distance;
		var d2 = Vector3.Dot(B, normal) - distance;
		var d3 = Vector3.Dot(C, normal) - distance;

		if(d1 * d2 < 0) return Slice(A, B, C, d1, d2, d3);
		if(d1 * d3 < 0) return Slice(C, A, B, d3, d1, d2);
		if(d2 * d3 < 0) return Slice(B, C, A, d2, d3, d1);
		return new[] { this };
	}

	static IEnumerable<Triangle3D> Slice(Vector3 a, Vector3 b, Vector3 c, float d1, float d2, float d3) {
		var ab = a + d1 / (d1 - d2) * (b - a);
		if(d1 < 0) {
			if(d3 < 0) {
				var bc = b + d2 / (d2 - d3) * (c - b);
				yield return new Triangle3D(b, bc, ab);
				yield return new Triangle3D(bc, c, a);
				yield return new Triangle3D(ab, bc, a);
			} else {
				var ac = a + d1 / (d1 - d3) * (c - a);
				yield return new Triangle3D(a, ab, ac);
				yield return new Triangle3D(ab, b, c);
				yield return new Triangle3D(ac, ab, c);
			}
		} else {
			if(d3 < 0) {
				var ac = a + d1 / (d1 - d3) * (c - a);
				yield return new Triangle3D(a, ab, ac);
				yield return new Triangle3D(ac, ab, b);
				yield return new Triangle3D(b, c, ac);
			} else {
				var bc = b + d2 / (d2 - d3) * (c - b);
				yield return new Triangle3D(b, bc, ab);
				yield return new Triangle3D(a, ab, bc);
				yield return new Triangle3D(c, a, bc);
			}
		}
	}

	public IEnumerable<(Vector3 A, Vector3 B)> ClipSegment((Vector3 A, Vector3 B) x) {
		var points = new List<Vector3> { x.A, x.B };
		points.Sort((p1, p2) => (p1 - x.A).LengthSquared().CompareTo((p2 - x.B).LengthSquared()));
		for(var i = 0; i < points.Count - 1; ++i)
			yield return (points[i], points[i + 1]);
	}
}