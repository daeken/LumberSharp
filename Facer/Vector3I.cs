using System.Numerics;
using static System.MathF;

namespace Facer;

readonly struct Vector3I(Vector3 v, float scale = 10f) : IEquatable<Vector3I> {
	public readonly int
		X = (int) Round(v.X * scale),
		Y = (int) Round(v.Y * scale),
		Z = (int) Round(v.Z * scale);
	public readonly Vector3 Vector3 = v;
	public bool Equals(Vector3I other) => X == other.X && Y == other.Y && Z == other.Z;
	public override bool Equals(object obj) => obj is Vector3I other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(X, Y, Z);

	public static bool operator ==(Vector3I a, Vector3I b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z;
	public static bool operator !=(Vector3I a, Vector3I b) => a.X != b.X || a.Y != b.Y || a.Z != b.Z;
}