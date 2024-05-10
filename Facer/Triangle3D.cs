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
}