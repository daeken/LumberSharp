using System.Numerics;
using DoubleSharp.MathPlus;

namespace SdfLib;
using static Common.Helpers;
using static MathF;
using static Vector3;
using SDF = Func<Vector3, float>;

public static class Sdf3D {
	public const float Epsilon = 0.001f;
	
	public static (
			Func<Vector2, float> Sliced, 
			Func<Vector2, Vector3> UpDimension
	) Slice(Func<Vector3, float> scene, Vector3 planeNormal, float planeOffset) {
		var planeOrigin = planeNormal * planeOffset;
		var xAxis = (planeNormal - UnitX).LengthSquared() > Epsilon
			? UnitX
			: (planeNormal - UnitZ).LengthSquared() > Epsilon
				? UnitZ
				: UnitY;
		xAxis = Normalize(Cross(planeNormal, xAxis));
		var yAxis = Normalize(Cross(planeNormal, xAxis));
		(yAxis, xAxis) = (xAxis, yAxis);
		var upDimension = (Func<Vector2, Vector3>) (p => p.X * xAxis + p.Y * yAxis + planeOrigin);
		return (upDimension.Compose(scene), upDimension);
	}
	
	public static (Vector3 Center, float Radius) FindBoundingSphere(SDF f, int radialTestPoints = 60, float startRadius = 100f) {
		var rotPerTest = Tau / radialTestPoints;
		bool TestOutside(Vector3 o, float r, out float minDist, out Vector3 averageDirection) {
			averageDirection = Zero;
			minDist = float.PositiveInfinity;
			var directions = new List<Vector3>();
			var op = new Vector3(r + 0.01f, 0, 0);
			var ip = new Vector3(r, 0, 0);
			var rotY = 0f;
			for(var i = 0; i < radialTestPoints; ++i, rotY += rotPerTest) {
				var opy = op.RotateY(rotY);
				var ipy = ip.RotateY(rotY);
				var rotZ = 0f;
				for(var j = 0; j < radialTestPoints; ++j, rotZ += rotPerTest) {
					var rop = opy.RotateZ(rotZ) + o;
					var rip = ipy.RotateZ(rotZ) + o;
					var od = f(rop);
					var id = f(rip);
					minDist = Min(minDist, id);
					if(od < id || id < 0) return false;
					directions.Add((rip - rop).Normalize() * id);
				}
			}
			averageDirection = directions
				.Select(x => x / radialTestPoints / radialTestPoints)
				.Aggregate((a, b) => a + b);
			return true;
		}

		var origin = Zero;
		var radius = startRadius;
		while(true) {
			if(float.IsNaN(radius) || radius < 0)
				return (Zero, 0);
			if(TestOutside(origin, radius, out var minDist, out var avgDir)) {
				var offset = avgDir.Length();
				if(minDist < 0.01f && offset < 0.01f)
					break;
				origin += avgDir / 10;
				radius -= minDist / 10;
			} else
				radius *= 2;
		}
		return (origin, radius);
	}

	public static IEnumerable<Vector3> EvenlySampleSphere(
		Vector3 origin, float radius,
		int pointsPerAxis = 60
	) {
		var rotPerTest = Tau / pointsPerAxis;
		var p = new Vector3(radius, 0, 0);
		var rotY = 0f;
		for(var i = 0; i < pointsPerAxis; ++i, rotY += rotPerTest) {
			var py = p.RotateY(rotY);
			var rotZ = 0f;
			for(var j = 0; j < pointsPerAxis; ++j, rotZ += rotPerTest)
				yield return py.RotateZ(rotZ) + origin;
		}
	}

	public static Vector3 FindClosestSurfacePoint(Func<Vector3, float> f, Vector3 p) {
		for(var i = 0; i < 10000; ++i) {
			var d = f(p);
			if(d is >= 0 and <= Epsilon) break;
			if(d < 0)
				p -= FirstDerivative(f, p) * (d * 1.5f);
			else
				p -= FirstDerivative(f, p) * (d / 2);
		}
		return p;
	}

	public static Vector3 FirstDerivative(Func<Vector3, float> f, Vector3 p) =>
		Normalize(Gradient(f, p));

	public static Vector3 Gradient(Func<Vector3, float> f, Vector3 p) =>
		new(
			f(new(p.X + Epsilon, p.Y, p.Z)) - f(new(p.X - Epsilon, p.Y, p.Z)),
			f(new(p.X, p.Y + Epsilon, p.Z)) - f(new(p.X, p.Y - Epsilon, p.Z)), 
			f(new(p.X, p.Y, p.Z + Epsilon)) - f(new(p.X, p.Y, p.Z - Epsilon))
		);
	
	public static float Union(params float[] objs) => objs.Min();
	public static float Intersect(params float[] objs) => objs.Max();
	public static float Subtract(params float[] objs) => objs.Take(1).Concat(objs.Skip(1).Select(x => -x)).Max();

	public static float Twist(Vector3 p, Vector3 axis, float angle, Func<Vector3, float> f) =>
		f(Transform(p, Matrix4x4.CreateFromAxisAngle(axis, angle)));

	public static float Plane(Vector3 p, Vector3 n, float h) => Dot(p, n) + h;
	
	public static float Sphere(Vector3 p, float r) => p.Length() - r;

	public static float Box(Vector3 p, Vector3 b) {
		var q = p.Apply(Abs) - b;
		return Max(q, Zero).Length() + Min(q.Apply(Max), 0);
	}

	public static float Dodecahedron(Vector3 p, float r, float cr) {
		p = Abs(p);
		r -= cr;
		var phi = (1 + Sqrt(5)) * 0.5f;
		var a = 1 / Sqrt(3) * r;
		var b = 1 / Sqrt(3.0f) * r * (phi - 1.0f);
		var _a00 = new Vector3(a, 0, 0);
		var _0a0 = new Vector3(0, a, 0);
		var _a0a = new Vector3(a, 0, a);
		var _00a = new Vector3(0, 0, a);
		var aba = new Vector3(a, b, a);
		var b00 = new Vector3(b, 0, 0);
		var n1 = new Vector3(0, phi, 1) / Sqrt(phi + 2);
		var n2 = new Vector3(phi + 2, phi - 1, -1) / Sqrt(4 * phi + 8); 
		var n3 = new Vector3(phi, 1, 0) / Sqrt(phi + 2);
		var n4 = new Vector3(-1, phi, 3 - phi) / Sqrt(12 - 4 * phi);
		var p1 = p - _0a0;
		var h1 = Dot(p1 - _a0a, n1);
		var m1 = Dot(p1 - _a0a, n2);
		var d1 = p1 - Clamp(p1 - n1 * h1 - n2 * Max(m1, 0), Zero, aba); 
		var h2 = Dot(p1 - _a0a, n3);
		var m2 = Dot(p1 - _a0a, n4);
		var d2 = p1 - Clamp(p1 - n3 * h2 - n4 * Max(m2, 0), b00, aba);
		var p2 = (p - _a00).ZXY();
		var h3 = Dot(p2 - _a0a, n1);
		var m3 = Dot(p2 - _a0a, n2);
		var d3 = p2 - Clamp(p2 - n1 * h3 - n2 * Max(m3, 0), Zero, aba); 
		var h4 = Dot(p2 - _a0a, n3);
		var m4 = Dot(p2 - _a0a, n4);
		var d4 = p2 - Clamp(p2 - n3 * h4 - n4 * Max(m4, 0), b00, aba);
		var p3 = (p - _00a).YZX();
		var h5 = Dot(p3 - _a0a, n1);
		var m5 = Dot(p3 - _a0a, n2);
		var d5 = p3 - Clamp(p3 - n1 * h5 - n2 * Max(m5, 0), Zero, aba); 
		var h6 = Dot(p3 - _a0a, n3);
		var m6 = Dot(p3 - _a0a, n4);
		var d6 = p3 - Clamp(p3 - n3 * h6 - n4 * Max(m6, 0), b00, aba);
		var d = Sqrt(Min(Min(Min(Min(Min(Dot(d1, d1), Dot(d2, d2)), Dot(d3, d3)), Dot(d4, d4)), Dot(d5, d5)), Dot(d6, d6)));
		var s = Max(Max(Max(Max(Max(h1, h2), h3), h4), h5), h6);
		return (s < 0.0 ? -d : d) - cr;
	}
}