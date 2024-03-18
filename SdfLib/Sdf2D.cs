using System.Numerics;
using DoubleSharp.MathPlus;

namespace SdfLib;
using static Common.Helpers;
using static MathF;
using static Vector2;
using SDF = Func<Vector2, float>;

public static class Sdf2D {
	const float Epsilon = 0.001f;
	
	public static (Vector2 Center, float Radius) FindBoundingCircle(SDF f, int radialTestPoints = 360, float startRadius = 100f) {
		var rotPerTest = Tau / radialTestPoints;
		bool TestOutside(Vector2 o, float r, out float minDist, out Vector2 averageDirection) {
			averageDirection = Zero;
			minDist = float.PositiveInfinity;
			var directions = new List<Vector2>();
			var op = new Vector2(r + 0.01f, 0);
			var ip = new Vector2(r, 0);
			float rot = 0;
			for(var i = 0; i < radialTestPoints; ++i, rot += rotPerTest) {
				var rop = op.Rotate(rot) + o;
				var rip = ip.Rotate(rot) + o;
				var od = f(rop);
				var id = f(rip);
				minDist = Min(minDist, id);
				if(od < id || id < 0) return false;
				directions.Add((rip - rop).Normalize() * id);
			}
			averageDirection = directions
				.Select(x => x / radialTestPoints)
				.Aggregate((a, b) => a + b);
			return true;
		}

		var origin = Zero;
		var radius = startRadius;
		for(var i = 0; i <= 100; ++i) {
			if(i == 100)
				return (origin, radius * 2);
			if(float.IsNaN(radius) || radius < 0.01f)
				return (Zero, 0);
			if(TestOutside(origin, radius, out var minDist, out var avgDir)) {
				var offset = avgDir.Length();
				if(minDist < 0.01f && offset < 0.01f)
					break;
				origin += avgDir / 10;
				radius -= minDist / 3;
			} else
				radius *= 2;
		}
		return (origin, radius);
	}
	
	public static Vector2 FindClosestSurfacePoint(Func<Vector2, float> f, Vector2 p) {
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
	
	public static Vector2 FirstDerivative(Func<Vector2, float> f, Vector2 p) =>
		Normalize(Gradient(f, p));
		
	public static Vector2 Gradient(Func<Vector2, float> f, Vector2 p) =>
		new(
			f(new(p.X + Epsilon, p.Y)) - f(new(p.X - Epsilon, p.Y)),
			f(new(p.X, p.Y + Epsilon)) - f(new(p.X, p.Y - Epsilon)) 
		);
}