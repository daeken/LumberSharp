using System.Diagnostics.Contracts;
using System.Numerics;

namespace Facer;

public class AABB {
	static readonly Vector3[] BoxNormals = [
		new(1, 0, 0),
		new(0, 1, 0),
		new(0, 0, 1)
	];

	public readonly Vector3 Center;

	public readonly Vector3 Min, Max;
	public readonly Vector3 Size;

	public AABB(Vector3 min, Vector3 max) {
		Min = min;
		Max = max;
		Size = max - min;
		Center = (min + max) / 2;
	}

	public bool Contains(Triangle3D tri) {
		return Contains(tri.A) && Contains(tri.B) && Contains(tri.C);
	}

	public bool StrictlyContains(Triangle3D tri) {
		return StrictlyContains(tri.A) && StrictlyContains(tri.B) && StrictlyContains(tri.C);
	}

	public bool IntersectedBy(Triangle3D tri) {
		ReadOnlySpan<Vector3> triVerts = [tri.A, tri.B, tri.C];
		var (triangleMin, triangleMax) = Project(triVerts, new(1, 0, 0));
		if(triangleMax < Min.X || triangleMin > Max.X) return false;
		(triangleMin, triangleMax) = Project(triVerts, new(0, 1, 0));
		if(triangleMax < Min.Y || triangleMin > Max.Y) return false;
		(triangleMin, triangleMax) = Project(triVerts, new(0, 0, 1));
		if(triangleMax < Min.Z || triangleMin > Max.Z) return false;

		ReadOnlySpan<Vector3> boxVerts = [
			Min,
			new(Max.X, Min.Y, Min.Z),
			new(Min.X, Max.Y, Min.Z),
			new(Max.X, Max.Y, Min.Z),

			new(Min.X, Min.Y, Max.Z),
			new(Max.X, Min.Y, Max.Z),
			new(Min.X, Max.Y, Max.Z),
			new(Max.X, Max.Y, Max.Z)
		];

		var triangleOffset = Vector3.Dot(tri.Normal, tri.A);
		var (boxMin, boxMax) = Project(boxVerts, tri.Normal);
		if(boxMax < triangleOffset || boxMin > triangleOffset) return false;

		ReadOnlySpan<Vector3> triangleEdges = [
			tri.A - tri.B,
			tri.B - tri.C,
			tri.C - tri.A
		];
		foreach(var edge in triangleEdges) {
			foreach(var boxNormal in BoxNormals) {
				var axis = Vector3.Cross(edge, boxNormal);
				(boxMin, boxMax) = Project(boxVerts, axis);
				(triangleMin, triangleMax) = Project(triVerts, axis);
				if(boxMax <= triangleMin || boxMin >= triangleMax) return false;
			}
		}

		return true;
	}

	static (float Min, float Max) Project(ReadOnlySpan<Vector3> points, Vector3 axis) {
		var min = float.PositiveInfinity;
		var max = float.NegativeInfinity;

		foreach(var p in points) {
			var val = Vector3.Dot(axis, p);
			if(val < min) min = val;
			if(val > max) max = val;
		}

		return (min, max);
	}

	[Pure]
	public bool Contains(Vector3 point) {
		return Min.X <= point.X && Min.Y <= point.Y && Min.Z <= point.Z &&
		       Max.X >= point.X && Max.Y >= point.Y && Max.Z >= point.Z;
	}

	public bool StrictlyContains(Vector3 point) {
		return Min.X < point.X && Min.Y < point.Y && Min.Z < point.Z &&
		       Max.X > point.X && Max.Y > point.Y && Max.Z > point.Z;
	}

	public bool IntersectedBy(Vector3 origin, Vector3 direction) {
		if(Contains(origin)) return true;

		var tmin = (Min.X - origin.X) / direction.X;
		var tmax = (Max.X - origin.X) / direction.X;
		if(tmin > tmax)
			(tmin, tmax) = (tmax, tmin);

		var tymin = (Min.Y - origin.Y) / direction.Y;
		var tymax = (Max.Y - origin.Y) / direction.Y;
		if(tymin > tymax)
			(tymin, tymax) = (tymax, tymin);

		if(tmin > tymax || tymin > tmax) return false;

		if(tymin > tmin) tmin = tymin;
		if(tymax < tmax) tmax = tymax;

		var tzmin = (Min.Z - origin.Z) / direction.Z;
		var tzmax = (Max.Z - origin.Z) / direction.Z;
		if(tzmin > tzmax)
			(tzmin, tzmax) = (tzmax, tzmin);

		return tmin <= tzmax && tzmin <= tmax;
	}

	public override string ToString() {
		return $"AABB(Min={Min}, Max={Max})";
	}
}
