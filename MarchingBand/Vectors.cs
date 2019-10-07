using System.Numerics;

namespace MarchingBand {
	public class Vec2 {
		public Vector2 V;
		public static readonly Vec2 Zero = new Vec2();

		public float x {
			get => V.X;
			set => V.X = value;
		}

		public float y {
			get => V.Y;
			set => V.Y = value;
		}

		public float Length => V.Length();
		public Vec2 Normalized => new Vec2(Vector2.Normalize(V));

		public Vec2() => V = Vector2.Zero;
		public Vec2(float v) => V = new Vector2(v);
		public Vec2(float x, float y) => V = new Vector2(x, y);
		public Vec2(Vector2 v) => V = v;

		public static Vec2 operator +(Vec2 left, Vec2 right) => new Vec2(left.x + right.x, left.y + right.y);
		public static Vec2 operator +(Vec2 left, float right) => new Vec2(left.x + right, left.y + right);
		public static Vec2 operator +(float left, Vec2 right) => new Vec2(left + right.x, left + right.y);

		public static Vec2 operator -(Vec2 left, Vec2 right) => new Vec2(left.x - right.x, left.y - right.y);
		public static Vec2 operator -(Vec2 left, float right) => new Vec2(left.x - right, left.y - right);
		public static Vec2 operator -(float left, Vec2 right) => new Vec2(left - right.x, left - right.y);

		public static Vec2 operator *(Vec2 left, Vec2 right) => new Vec2(left.x * right.x, left.y * right.y);
		public static Vec2 operator *(Vec2 left, float right) => new Vec2(left.x * right, left.y * right);
		public static Vec2 operator *(float left, Vec2 right) => new Vec2(left * right.x, left * right.y);

		public static Vec2 operator /(Vec2 left, Vec2 right) => new Vec2(left.x / right.x, left.y / right.y);
		public static Vec2 operator /(Vec2 left, float right) => new Vec2(left.x / right, left.y / right);
		public static Vec2 operator /(float left, Vec2 right) => new Vec2(left / right.x, left / right.y);
		
		public Vec2 xx => new Vec2(x, x);
		public Vec2 xy => this;
		public Vec2 yx => new Vec2(y, x);
		public Vec2 yy => new Vec2(y, y);

		public Vec3 xxx => new Vec3(x, x, x);
		public Vec3 xxy => new Vec3(x, x, y);
		public Vec3 xyx => new Vec3(x, y, x);
		public Vec3 xyy => new Vec3(x, y, y);
		public Vec3 yxx => new Vec3(y, x, x);
		public Vec3 yxy => new Vec3(y, x, y);
		public Vec3 yyx => new Vec3(y, y, x);
		public Vec3 yyy => new Vec3(y, y, y);

		public Vec4 xxxx => new Vec4(x, x, x, x);
		public Vec4 xxxy => new Vec4(x, x, x, y);
		public Vec4 xxyx => new Vec4(x, x, y, x);
		public Vec4 xxyy => new Vec4(x, x, y, y);
		public Vec4 xyxx => new Vec4(x, y, x, x);
		public Vec4 xyxy => new Vec4(x, y, x, y);
		public Vec4 xyyx => new Vec4(x, y, y, x);
		public Vec4 xyyy => new Vec4(x, y, y, y);
		public Vec4 yxxx => new Vec4(y, x, x, x);
		public Vec4 yxxy => new Vec4(y, x, x, y);
		public Vec4 yxyx => new Vec4(y, x, y, x);
		public Vec4 yxyy => new Vec4(y, x, y, y);
		public Vec4 yyxx => new Vec4(y, y, x, x);
		public Vec4 yyxy => new Vec4(y, y, x, y);
		public Vec4 yyyx => new Vec4(y, y, y, x);
		public Vec4 yyyy => new Vec4(y, y, y, y);
	}

	public class Vec3 {
		public Vector3 V;
		public static readonly Vec3 Zero = new Vec3();

		public float x {
			get => V.X;
			set => V.X = value;
		}

		public float y {
			get => V.Y;
			set => V.Y = value;
		}

		public float z {
			get => V.Z;
			set => V.Z = value;
		}

		public float Length => V.Length();
		public Vec3 Normalized => new Vec3(Vector3.Normalize(V));

		public Vec3() => V = Vector3.Zero;
		public Vec3(float v) => V = new Vector3(v);
		public Vec3(float x, float y, float z) => V = new Vector3(x, y, z);
		public Vec3(Vector3 v) => V = v;

		public static Vec3 operator +(Vec3 left, Vec3 right) =>
			new Vec3(left.x + right.x, left.y + right.y, left.z + right.z);

		public static Vec3 operator +(Vec3 left, float right) =>
			new Vec3(left.x + right, left.y + right, left.z + right);

		public static Vec3 operator +(float left, Vec3 right) =>
			new Vec3(left + right.x, left + right.y, left + right.z);

		public static Vec3 operator -(Vec3 left, Vec3 right) =>
			new Vec3(left.x - right.x, left.y - right.y, left.z - right.z);

		public static Vec3 operator -(Vec3 left, float right) =>
			new Vec3(left.x - right, left.y - right, left.z - right);

		public static Vec3 operator -(float left, Vec3 right) =>
			new Vec3(left - right.x, left - right.y, left - right.z);

		public static Vec3 operator *(Vec3 left, Vec3 right) =>
			new Vec3(left.x * right.x, left.y * right.y, left.z * right.z);

		public static Vec3 operator *(Vec3 left, float right) =>
			new Vec3(left.x * right, left.y * right, left.z * right);

		public static Vec3 operator *(float left, Vec3 right) =>
			new Vec3(left * right.x, left * right.y, left * right.z);

		public static Vec3 operator /(Vec3 left, Vec3 right) =>
			new Vec3(left.x / right.x, left.y / right.y, left.z / right.z);

		public static Vec3 operator /(Vec3 left, float right) =>
			new Vec3(left.x / right, left.y / right, left.z / right);

		public static Vec3 operator /(float left, Vec3 right) =>
			new Vec3(left / right.x, left / right.y, left / right.z);
		
		public Vec2 xx => new Vec2(x, x);
		public Vec2 xy => new Vec2(x, y);
		public Vec2 xz => new Vec2(x, z);
		public Vec2 yx => new Vec2(y, x);
		public Vec2 yy => new Vec2(y, y);
		public Vec2 yz => new Vec2(y, z);
		public Vec2 zx => new Vec2(z, x);
		public Vec2 zy => new Vec2(z, y);
		public Vec2 zz => new Vec2(z, z);

		public Vec3 xxx => new Vec3(x, x, x);
		public Vec3 xxy => new Vec3(x, x, y);
		public Vec3 xxz => new Vec3(x, x, z);
		public Vec3 xyx => new Vec3(x, y, x);
		public Vec3 xyy => new Vec3(x, y, y);
		public Vec3 xyz => this;
		public Vec3 xzx => new Vec3(x, z, x);
		public Vec3 xzy => new Vec3(x, z, y);
		public Vec3 xzz => new Vec3(x, z, z);
		public Vec3 yxx => new Vec3(y, x, x);
		public Vec3 yxy => new Vec3(y, x, y);
		public Vec3 yxz => new Vec3(y, x, z);
		public Vec3 yyx => new Vec3(y, y, x);
		public Vec3 yyy => new Vec3(y, y, y);
		public Vec3 yyz => new Vec3(y, y, z);
		public Vec3 yzx => new Vec3(y, z, x);
		public Vec3 yzy => new Vec3(y, z, y);
		public Vec3 yzz => new Vec3(y, z, z);
		public Vec3 zxx => new Vec3(z, x, x);
		public Vec3 zxy => new Vec3(z, x, y);
		public Vec3 zxz => new Vec3(z, x, z);
		public Vec3 zyx => new Vec3(z, y, x);
		public Vec3 zyy => new Vec3(z, y, y);
		public Vec3 zyz => new Vec3(z, y, z);
		public Vec3 zzx => new Vec3(z, z, x);
		public Vec3 zzy => new Vec3(z, z, y);
		public Vec3 zzz => new Vec3(z, z, z);

		public Vec4 xxxx => new Vec4(x, x, x, x);
		public Vec4 xxxy => new Vec4(x, x, x, y);
		public Vec4 xxxz => new Vec4(x, x, x, z);
		public Vec4 xxyx => new Vec4(x, x, y, x);
		public Vec4 xxyy => new Vec4(x, x, y, y);
		public Vec4 xxyz => new Vec4(x, x, y, z);
		public Vec4 xxzx => new Vec4(x, x, z, x);
		public Vec4 xxzy => new Vec4(x, x, z, y);
		public Vec4 xxzz => new Vec4(x, x, z, z);
		public Vec4 xyxx => new Vec4(x, y, x, x);
		public Vec4 xyxy => new Vec4(x, y, x, y);
		public Vec4 xyxz => new Vec4(x, y, x, z);
		public Vec4 xyyx => new Vec4(x, y, y, x);
		public Vec4 xyyy => new Vec4(x, y, y, y);
		public Vec4 xyyz => new Vec4(x, y, y, z);
		public Vec4 xyzx => new Vec4(x, y, z, x);
		public Vec4 xyzy => new Vec4(x, y, z, y);
		public Vec4 xyzz => new Vec4(x, y, z, z);
		public Vec4 xzxx => new Vec4(x, z, x, x);
		public Vec4 xzxy => new Vec4(x, z, x, y);
		public Vec4 xzxz => new Vec4(x, z, x, z);
		public Vec4 xzyx => new Vec4(x, z, y, x);
		public Vec4 xzyy => new Vec4(x, z, y, y);
		public Vec4 xzyz => new Vec4(x, z, y, z);
		public Vec4 xzzx => new Vec4(x, z, z, x);
		public Vec4 xzzy => new Vec4(x, z, z, y);
		public Vec4 xzzz => new Vec4(x, z, z, z);
		public Vec4 yxxx => new Vec4(y, x, x, x);
		public Vec4 yxxy => new Vec4(y, x, x, y);
		public Vec4 yxxz => new Vec4(y, x, x, z);
		public Vec4 yxyx => new Vec4(y, x, y, x);
		public Vec4 yxyy => new Vec4(y, x, y, y);
		public Vec4 yxyz => new Vec4(y, x, y, z);
		public Vec4 yxzx => new Vec4(y, x, z, x);
		public Vec4 yxzy => new Vec4(y, x, z, y);
		public Vec4 yxzz => new Vec4(y, x, z, z);
		public Vec4 yyxx => new Vec4(y, y, x, x);
		public Vec4 yyxy => new Vec4(y, y, x, y);
		public Vec4 yyxz => new Vec4(y, y, x, z);
		public Vec4 yyyx => new Vec4(y, y, y, x);
		public Vec4 yyyy => new Vec4(y, y, y, y);
		public Vec4 yyyz => new Vec4(y, y, y, z);
		public Vec4 yyzx => new Vec4(y, y, z, x);
		public Vec4 yyzy => new Vec4(y, y, z, y);
		public Vec4 yyzz => new Vec4(y, y, z, z);
		public Vec4 yzxx => new Vec4(y, z, x, x);
		public Vec4 yzxy => new Vec4(y, z, x, y);
		public Vec4 yzxz => new Vec4(y, z, x, z);
		public Vec4 yzyx => new Vec4(y, z, y, x);
		public Vec4 yzyy => new Vec4(y, z, y, y);
		public Vec4 yzyz => new Vec4(y, z, y, z);
		public Vec4 yzzx => new Vec4(y, z, z, x);
		public Vec4 yzzy => new Vec4(y, z, z, y);
		public Vec4 yzzz => new Vec4(y, z, z, z);
		public Vec4 zxxx => new Vec4(z, x, x, x);
		public Vec4 zxxy => new Vec4(z, x, x, y);
		public Vec4 zxxz => new Vec4(z, x, x, z);
		public Vec4 zxyx => new Vec4(z, x, y, x);
		public Vec4 zxyy => new Vec4(z, x, y, y);
		public Vec4 zxyz => new Vec4(z, x, y, z);
		public Vec4 zxzx => new Vec4(z, x, z, x);
		public Vec4 zxzy => new Vec4(z, x, z, y);
		public Vec4 zxzz => new Vec4(z, x, z, z);
		public Vec4 zyxx => new Vec4(z, y, x, x);
		public Vec4 zyxy => new Vec4(z, y, x, y);
		public Vec4 zyxz => new Vec4(z, y, x, z);
		public Vec4 zyyx => new Vec4(z, y, y, x);
		public Vec4 zyyy => new Vec4(z, y, y, y);
		public Vec4 zyyz => new Vec4(z, y, y, z);
		public Vec4 zyzx => new Vec4(z, y, z, x);
		public Vec4 zyzy => new Vec4(z, y, z, y);
		public Vec4 zyzz => new Vec4(z, y, z, z);
		public Vec4 zzxx => new Vec4(z, z, x, x);
		public Vec4 zzxy => new Vec4(z, z, x, y);
		public Vec4 zzxz => new Vec4(z, z, x, z);
		public Vec4 zzyx => new Vec4(z, z, y, x);
		public Vec4 zzyy => new Vec4(z, z, y, y);
		public Vec4 zzyz => new Vec4(z, z, y, z);
		public Vec4 zzzx => new Vec4(z, z, z, x);
		public Vec4 zzzy => new Vec4(z, z, z, y);
		public Vec4 zzzz => new Vec4(z, z, z, z);
	}

	public class Vec4 {
		public Vector4 V;
		public static readonly Vec4 Zero = new Vec4();

		public float x {
			get => V.X;
			set => V.X = value;
		}

		public float y {
			get => V.Y;
			set => V.Y = value;
		}

		public float z {
			get => V.Z;
			set => V.Z = value;
		}

		public float w {
			get => V.W;
			set => V.W = value;
		}

		public float Length => V.Length();
		public Vec4 Normalized => new Vec4(Vector4.Normalize(V));

		public Vec4() => V = Vector4.Zero;
		public Vec4(float v) => V = new Vector4(v);
		public Vec4(float x, float y, float z, float w) => V = new Vector4(x, y, z, w);
		public Vec4(Vector4 v) => V = v;

		public static Vec4 operator +(Vec4 left, Vec4 right) =>
			new Vec4(left.x + right.x, left.y + right.y, left.z + right.z, left.w + right.w);

		public static Vec4 operator +(Vec4 left, float right) =>
			new Vec4(left.x + right, left.y + right, left.z + right, left.w + right);

		public static Vec4 operator +(float left, Vec4 right) =>
			new Vec4(left + right.x, left + right.y, left + right.z, left + right.w);

		public static Vec4 operator -(Vec4 left, Vec4 right) =>
			new Vec4(left.x - right.x, left.y - right.y, left.z - right.z, left.w - right.w);

		public static Vec4 operator -(Vec4 left, float right) =>
			new Vec4(left.x - right, left.y - right, left.z - right, left.w - right);

		public static Vec4 operator -(float left, Vec4 right) =>
			new Vec4(left - right.x, left - right.y, left - right.z, left - right.w);

		public static Vec4 operator *(Vec4 left, Vec4 right) =>
			new Vec4(left.x * right.x, left.y * right.y, left.z * right.z, left.w * right.w);

		public static Vec4 operator *(Vec4 left, float right) =>
			new Vec4(left.x * right, left.y * right, left.z * right, left.w * right);

		public static Vec4 operator *(float left, Vec4 right) =>
			new Vec4(left * right.x, left * right.y, left * right.z, left * right.w);

		public static Vec4 operator /(Vec4 left, Vec4 right) =>
			new Vec4(left.x / right.x, left.y / right.y, left.z / right.z, left.w / right.w);

		public static Vec4 operator /(Vec4 left, float right) =>
			new Vec4(left.x / right, left.y / right, left.z / right, left.w / right);

		public static Vec4 operator /(float left, Vec4 right) =>
			new Vec4(left / right.x, left / right.y, left / right.z, left / right.w);

		public static Vec4 operator *(Matrix4x4 left, Vec4 right) => new Vec4(Vector4.Transform(right.V, left));
		
		public Vec2 xx => new Vec2(x, x);
		public Vec2 xy => new Vec2(x, y);
		public Vec2 xz => new Vec2(x, z);
		public Vec2 xw => new Vec2(x, w);
		public Vec2 yx => new Vec2(y, x);
		public Vec2 yy => new Vec2(y, y);
		public Vec2 yz => new Vec2(y, z);
		public Vec2 yw => new Vec2(y, w);
		public Vec2 zx => new Vec2(z, x);
		public Vec2 zy => new Vec2(z, y);
		public Vec2 zz => new Vec2(z, z);
		public Vec2 zw => new Vec2(z, w);
		public Vec2 wx => new Vec2(w, x);
		public Vec2 wy => new Vec2(w, y);
		public Vec2 wz => new Vec2(w, z);
		public Vec2 ww => new Vec2(w, w);

		public Vec3 xxx => new Vec3(x, x, x);
		public Vec3 xxy => new Vec3(x, x, y);
		public Vec3 xxz => new Vec3(x, x, z);
		public Vec3 xxw => new Vec3(x, x, w);
		public Vec3 xyx => new Vec3(x, y, x);
		public Vec3 xyy => new Vec3(x, y, y);
		public Vec3 xyz => new Vec3(x, y, z);
		public Vec3 xyw => new Vec3(x, y, w);
		public Vec3 xzx => new Vec3(x, z, x);
		public Vec3 xzy => new Vec3(x, z, y);
		public Vec3 xzz => new Vec3(x, z, z);
		public Vec3 xzw => new Vec3(x, z, w);
		public Vec3 xwx => new Vec3(x, w, x);
		public Vec3 xwy => new Vec3(x, w, y);
		public Vec3 xwz => new Vec3(x, w, z);
		public Vec3 xww => new Vec3(x, w, w);
		public Vec3 yxx => new Vec3(y, x, x);
		public Vec3 yxy => new Vec3(y, x, y);
		public Vec3 yxz => new Vec3(y, x, z);
		public Vec3 yxw => new Vec3(y, x, w);
		public Vec3 yyx => new Vec3(y, y, x);
		public Vec3 yyy => new Vec3(y, y, y);
		public Vec3 yyz => new Vec3(y, y, z);
		public Vec3 yyw => new Vec3(y, y, w);
		public Vec3 yzx => new Vec3(y, z, x);
		public Vec3 yzy => new Vec3(y, z, y);
		public Vec3 yzz => new Vec3(y, z, z);
		public Vec3 yzw => new Vec3(y, z, w);
		public Vec3 ywx => new Vec3(y, w, x);
		public Vec3 ywy => new Vec3(y, w, y);
		public Vec3 ywz => new Vec3(y, w, z);
		public Vec3 yww => new Vec3(y, w, w);
		public Vec3 zxx => new Vec3(z, x, x);
		public Vec3 zxy => new Vec3(z, x, y);
		public Vec3 zxz => new Vec3(z, x, z);
		public Vec3 zxw => new Vec3(z, x, w);
		public Vec3 zyx => new Vec3(z, y, x);
		public Vec3 zyy => new Vec3(z, y, y);
		public Vec3 zyz => new Vec3(z, y, z);
		public Vec3 zyw => new Vec3(z, y, w);
		public Vec3 zzx => new Vec3(z, z, x);
		public Vec3 zzy => new Vec3(z, z, y);
		public Vec3 zzz => new Vec3(z, z, z);
		public Vec3 zzw => new Vec3(z, z, w);
		public Vec3 zwx => new Vec3(z, w, x);
		public Vec3 zwy => new Vec3(z, w, y);
		public Vec3 zwz => new Vec3(z, w, z);
		public Vec3 zww => new Vec3(z, w, w);
		public Vec3 wxx => new Vec3(w, x, x);
		public Vec3 wxy => new Vec3(w, x, y);
		public Vec3 wxz => new Vec3(w, x, z);
		public Vec3 wxw => new Vec3(w, x, w);
		public Vec3 wyx => new Vec3(w, y, x);
		public Vec3 wyy => new Vec3(w, y, y);
		public Vec3 wyz => new Vec3(w, y, z);
		public Vec3 wyw => new Vec3(w, y, w);
		public Vec3 wzx => new Vec3(w, z, x);
		public Vec3 wzy => new Vec3(w, z, y);
		public Vec3 wzz => new Vec3(w, z, z);
		public Vec3 wzw => new Vec3(w, z, w);
		public Vec3 wwx => new Vec3(w, w, x);
		public Vec3 wwy => new Vec3(w, w, y);
		public Vec3 wwz => new Vec3(w, w, z);
		public Vec3 www => new Vec3(w, w, w);

		public Vec4 xxxx => new Vec4(x, x, x, x);
		public Vec4 xxxy => new Vec4(x, x, x, y);
		public Vec4 xxxz => new Vec4(x, x, x, z);
		public Vec4 xxxw => new Vec4(x, x, x, w);
		public Vec4 xxyx => new Vec4(x, x, y, x);
		public Vec4 xxyy => new Vec4(x, x, y, y);
		public Vec4 xxyz => new Vec4(x, x, y, z);
		public Vec4 xxyw => new Vec4(x, x, y, w);
		public Vec4 xxzx => new Vec4(x, x, z, x);
		public Vec4 xxzy => new Vec4(x, x, z, y);
		public Vec4 xxzz => new Vec4(x, x, z, z);
		public Vec4 xxzw => new Vec4(x, x, z, w);
		public Vec4 xxwx => new Vec4(x, x, w, x);
		public Vec4 xxwy => new Vec4(x, x, w, y);
		public Vec4 xxwz => new Vec4(x, x, w, z);
		public Vec4 xxww => new Vec4(x, x, w, w);
		public Vec4 xyxx => new Vec4(x, y, x, x);
		public Vec4 xyxy => new Vec4(x, y, x, y);
		public Vec4 xyxz => new Vec4(x, y, x, z);
		public Vec4 xyxw => new Vec4(x, y, x, w);
		public Vec4 xyyx => new Vec4(x, y, y, x);
		public Vec4 xyyy => new Vec4(x, y, y, y);
		public Vec4 xyyz => new Vec4(x, y, y, z);
		public Vec4 xyyw => new Vec4(x, y, y, w);
		public Vec4 xyzx => new Vec4(x, y, z, x);
		public Vec4 xyzy => new Vec4(x, y, z, y);
		public Vec4 xyzz => new Vec4(x, y, z, z);
		public Vec4 xyzw => this;
		public Vec4 xywx => new Vec4(x, y, w, x);
		public Vec4 xywy => new Vec4(x, y, w, y);
		public Vec4 xywz => new Vec4(x, y, w, z);
		public Vec4 xyww => new Vec4(x, y, w, w);
		public Vec4 xzxx => new Vec4(x, z, x, x);
		public Vec4 xzxy => new Vec4(x, z, x, y);
		public Vec4 xzxz => new Vec4(x, z, x, z);
		public Vec4 xzxw => new Vec4(x, z, x, w);
		public Vec4 xzyx => new Vec4(x, z, y, x);
		public Vec4 xzyy => new Vec4(x, z, y, y);
		public Vec4 xzyz => new Vec4(x, z, y, z);
		public Vec4 xzyw => new Vec4(x, z, y, w);
		public Vec4 xzzx => new Vec4(x, z, z, x);
		public Vec4 xzzy => new Vec4(x, z, z, y);
		public Vec4 xzzz => new Vec4(x, z, z, z);
		public Vec4 xzzw => new Vec4(x, z, z, w);
		public Vec4 xzwx => new Vec4(x, z, w, x);
		public Vec4 xzwy => new Vec4(x, z, w, y);
		public Vec4 xzwz => new Vec4(x, z, w, z);
		public Vec4 xzww => new Vec4(x, z, w, w);
		public Vec4 xwxx => new Vec4(x, w, x, x);
		public Vec4 xwxy => new Vec4(x, w, x, y);
		public Vec4 xwxz => new Vec4(x, w, x, z);
		public Vec4 xwxw => new Vec4(x, w, x, w);
		public Vec4 xwyx => new Vec4(x, w, y, x);
		public Vec4 xwyy => new Vec4(x, w, y, y);
		public Vec4 xwyz => new Vec4(x, w, y, z);
		public Vec4 xwyw => new Vec4(x, w, y, w);
		public Vec4 xwzx => new Vec4(x, w, z, x);
		public Vec4 xwzy => new Vec4(x, w, z, y);
		public Vec4 xwzz => new Vec4(x, w, z, z);
		public Vec4 xwzw => new Vec4(x, w, z, w);
		public Vec4 xwwx => new Vec4(x, w, w, x);
		public Vec4 xwwy => new Vec4(x, w, w, y);
		public Vec4 xwwz => new Vec4(x, w, w, z);
		public Vec4 xwww => new Vec4(x, w, w, w);
		public Vec4 yxxx => new Vec4(y, x, x, x);
		public Vec4 yxxy => new Vec4(y, x, x, y);
		public Vec4 yxxz => new Vec4(y, x, x, z);
		public Vec4 yxxw => new Vec4(y, x, x, w);
		public Vec4 yxyx => new Vec4(y, x, y, x);
		public Vec4 yxyy => new Vec4(y, x, y, y);
		public Vec4 yxyz => new Vec4(y, x, y, z);
		public Vec4 yxyw => new Vec4(y, x, y, w);
		public Vec4 yxzx => new Vec4(y, x, z, x);
		public Vec4 yxzy => new Vec4(y, x, z, y);
		public Vec4 yxzz => new Vec4(y, x, z, z);
		public Vec4 yxzw => new Vec4(y, x, z, w);
		public Vec4 yxwx => new Vec4(y, x, w, x);
		public Vec4 yxwy => new Vec4(y, x, w, y);
		public Vec4 yxwz => new Vec4(y, x, w, z);
		public Vec4 yxww => new Vec4(y, x, w, w);
		public Vec4 yyxx => new Vec4(y, y, x, x);
		public Vec4 yyxy => new Vec4(y, y, x, y);
		public Vec4 yyxz => new Vec4(y, y, x, z);
		public Vec4 yyxw => new Vec4(y, y, x, w);
		public Vec4 yyyx => new Vec4(y, y, y, x);
		public Vec4 yyyy => new Vec4(y, y, y, y);
		public Vec4 yyyz => new Vec4(y, y, y, z);
		public Vec4 yyyw => new Vec4(y, y, y, w);
		public Vec4 yyzx => new Vec4(y, y, z, x);
		public Vec4 yyzy => new Vec4(y, y, z, y);
		public Vec4 yyzz => new Vec4(y, y, z, z);
		public Vec4 yyzw => new Vec4(y, y, z, w);
		public Vec4 yywx => new Vec4(y, y, w, x);
		public Vec4 yywy => new Vec4(y, y, w, y);
		public Vec4 yywz => new Vec4(y, y, w, z);
		public Vec4 yyww => new Vec4(y, y, w, w);
		public Vec4 yzxx => new Vec4(y, z, x, x);
		public Vec4 yzxy => new Vec4(y, z, x, y);
		public Vec4 yzxz => new Vec4(y, z, x, z);
		public Vec4 yzxw => new Vec4(y, z, x, w);
		public Vec4 yzyx => new Vec4(y, z, y, x);
		public Vec4 yzyy => new Vec4(y, z, y, y);
		public Vec4 yzyz => new Vec4(y, z, y, z);
		public Vec4 yzyw => new Vec4(y, z, y, w);
		public Vec4 yzzx => new Vec4(y, z, z, x);
		public Vec4 yzzy => new Vec4(y, z, z, y);
		public Vec4 yzzz => new Vec4(y, z, z, z);
		public Vec4 yzzw => new Vec4(y, z, z, w);
		public Vec4 yzwx => new Vec4(y, z, w, x);
		public Vec4 yzwy => new Vec4(y, z, w, y);
		public Vec4 yzwz => new Vec4(y, z, w, z);
		public Vec4 yzww => new Vec4(y, z, w, w);
		public Vec4 ywxx => new Vec4(y, w, x, x);
		public Vec4 ywxy => new Vec4(y, w, x, y);
		public Vec4 ywxz => new Vec4(y, w, x, z);
		public Vec4 ywxw => new Vec4(y, w, x, w);
		public Vec4 ywyx => new Vec4(y, w, y, x);
		public Vec4 ywyy => new Vec4(y, w, y, y);
		public Vec4 ywyz => new Vec4(y, w, y, z);
		public Vec4 ywyw => new Vec4(y, w, y, w);
		public Vec4 ywzx => new Vec4(y, w, z, x);
		public Vec4 ywzy => new Vec4(y, w, z, y);
		public Vec4 ywzz => new Vec4(y, w, z, z);
		public Vec4 ywzw => new Vec4(y, w, z, w);
		public Vec4 ywwx => new Vec4(y, w, w, x);
		public Vec4 ywwy => new Vec4(y, w, w, y);
		public Vec4 ywwz => new Vec4(y, w, w, z);
		public Vec4 ywww => new Vec4(y, w, w, w);
		public Vec4 zxxx => new Vec4(z, x, x, x);
		public Vec4 zxxy => new Vec4(z, x, x, y);
		public Vec4 zxxz => new Vec4(z, x, x, z);
		public Vec4 zxxw => new Vec4(z, x, x, w);
		public Vec4 zxyx => new Vec4(z, x, y, x);
		public Vec4 zxyy => new Vec4(z, x, y, y);
		public Vec4 zxyz => new Vec4(z, x, y, z);
		public Vec4 zxyw => new Vec4(z, x, y, w);
		public Vec4 zxzx => new Vec4(z, x, z, x);
		public Vec4 zxzy => new Vec4(z, x, z, y);
		public Vec4 zxzz => new Vec4(z, x, z, z);
		public Vec4 zxzw => new Vec4(z, x, z, w);
		public Vec4 zxwx => new Vec4(z, x, w, x);
		public Vec4 zxwy => new Vec4(z, x, w, y);
		public Vec4 zxwz => new Vec4(z, x, w, z);
		public Vec4 zxww => new Vec4(z, x, w, w);
		public Vec4 zyxx => new Vec4(z, y, x, x);
		public Vec4 zyxy => new Vec4(z, y, x, y);
		public Vec4 zyxz => new Vec4(z, y, x, z);
		public Vec4 zyxw => new Vec4(z, y, x, w);
		public Vec4 zyyx => new Vec4(z, y, y, x);
		public Vec4 zyyy => new Vec4(z, y, y, y);
		public Vec4 zyyz => new Vec4(z, y, y, z);
		public Vec4 zyyw => new Vec4(z, y, y, w);
		public Vec4 zyzx => new Vec4(z, y, z, x);
		public Vec4 zyzy => new Vec4(z, y, z, y);
		public Vec4 zyzz => new Vec4(z, y, z, z);
		public Vec4 zyzw => new Vec4(z, y, z, w);
		public Vec4 zywx => new Vec4(z, y, w, x);
		public Vec4 zywy => new Vec4(z, y, w, y);
		public Vec4 zywz => new Vec4(z, y, w, z);
		public Vec4 zyww => new Vec4(z, y, w, w);
		public Vec4 zzxx => new Vec4(z, z, x, x);
		public Vec4 zzxy => new Vec4(z, z, x, y);
		public Vec4 zzxz => new Vec4(z, z, x, z);
		public Vec4 zzxw => new Vec4(z, z, x, w);
		public Vec4 zzyx => new Vec4(z, z, y, x);
		public Vec4 zzyy => new Vec4(z, z, y, y);
		public Vec4 zzyz => new Vec4(z, z, y, z);
		public Vec4 zzyw => new Vec4(z, z, y, w);
		public Vec4 zzzx => new Vec4(z, z, z, x);
		public Vec4 zzzy => new Vec4(z, z, z, y);
		public Vec4 zzzz => new Vec4(z, z, z, z);
		public Vec4 zzzw => new Vec4(z, z, z, w);
		public Vec4 zzwx => new Vec4(z, z, w, x);
		public Vec4 zzwy => new Vec4(z, z, w, y);
		public Vec4 zzwz => new Vec4(z, z, w, z);
		public Vec4 zzww => new Vec4(z, z, w, w);
		public Vec4 zwxx => new Vec4(z, w, x, x);
		public Vec4 zwxy => new Vec4(z, w, x, y);
		public Vec4 zwxz => new Vec4(z, w, x, z);
		public Vec4 zwxw => new Vec4(z, w, x, w);
		public Vec4 zwyx => new Vec4(z, w, y, x);
		public Vec4 zwyy => new Vec4(z, w, y, y);
		public Vec4 zwyz => new Vec4(z, w, y, z);
		public Vec4 zwyw => new Vec4(z, w, y, w);
		public Vec4 zwzx => new Vec4(z, w, z, x);
		public Vec4 zwzy => new Vec4(z, w, z, y);
		public Vec4 zwzz => new Vec4(z, w, z, z);
		public Vec4 zwzw => new Vec4(z, w, z, w);
		public Vec4 zwwx => new Vec4(z, w, w, x);
		public Vec4 zwwy => new Vec4(z, w, w, y);
		public Vec4 zwwz => new Vec4(z, w, w, z);
		public Vec4 zwww => new Vec4(z, w, w, w);
		public Vec4 wxxx => new Vec4(w, x, x, x);
		public Vec4 wxxy => new Vec4(w, x, x, y);
		public Vec4 wxxz => new Vec4(w, x, x, z);
		public Vec4 wxxw => new Vec4(w, x, x, w);
		public Vec4 wxyx => new Vec4(w, x, y, x);
		public Vec4 wxyy => new Vec4(w, x, y, y);
		public Vec4 wxyz => new Vec4(w, x, y, z);
		public Vec4 wxyw => new Vec4(w, x, y, w);
		public Vec4 wxzx => new Vec4(w, x, z, x);
		public Vec4 wxzy => new Vec4(w, x, z, y);
		public Vec4 wxzz => new Vec4(w, x, z, z);
		public Vec4 wxzw => new Vec4(w, x, z, w);
		public Vec4 wxwx => new Vec4(w, x, w, x);
		public Vec4 wxwy => new Vec4(w, x, w, y);
		public Vec4 wxwz => new Vec4(w, x, w, z);
		public Vec4 wxww => new Vec4(w, x, w, w);
		public Vec4 wyxx => new Vec4(w, y, x, x);
		public Vec4 wyxy => new Vec4(w, y, x, y);
		public Vec4 wyxz => new Vec4(w, y, x, z);
		public Vec4 wyxw => new Vec4(w, y, x, w);
		public Vec4 wyyx => new Vec4(w, y, y, x);
		public Vec4 wyyy => new Vec4(w, y, y, y);
		public Vec4 wyyz => new Vec4(w, y, y, z);
		public Vec4 wyyw => new Vec4(w, y, y, w);
		public Vec4 wyzx => new Vec4(w, y, z, x);
		public Vec4 wyzy => new Vec4(w, y, z, y);
		public Vec4 wyzz => new Vec4(w, y, z, z);
		public Vec4 wyzw => new Vec4(w, y, z, w);
		public Vec4 wywx => new Vec4(w, y, w, x);
		public Vec4 wywy => new Vec4(w, y, w, y);
		public Vec4 wywz => new Vec4(w, y, w, z);
		public Vec4 wyww => new Vec4(w, y, w, w);
		public Vec4 wzxx => new Vec4(w, z, x, x);
		public Vec4 wzxy => new Vec4(w, z, x, y);
		public Vec4 wzxz => new Vec4(w, z, x, z);
		public Vec4 wzxw => new Vec4(w, z, x, w);
		public Vec4 wzyx => new Vec4(w, z, y, x);
		public Vec4 wzyy => new Vec4(w, z, y, y);
		public Vec4 wzyz => new Vec4(w, z, y, z);
		public Vec4 wzyw => new Vec4(w, z, y, w);
		public Vec4 wzzx => new Vec4(w, z, z, x);
		public Vec4 wzzy => new Vec4(w, z, z, y);
		public Vec4 wzzz => new Vec4(w, z, z, z);
		public Vec4 wzzw => new Vec4(w, z, z, w);
		public Vec4 wzwx => new Vec4(w, z, w, x);
		public Vec4 wzwy => new Vec4(w, z, w, y);
		public Vec4 wzwz => new Vec4(w, z, w, z);
		public Vec4 wzww => new Vec4(w, z, w, w);
		public Vec4 wwxx => new Vec4(w, w, x, x);
		public Vec4 wwxy => new Vec4(w, w, x, y);
		public Vec4 wwxz => new Vec4(w, w, x, z);
		public Vec4 wwxw => new Vec4(w, w, x, w);
		public Vec4 wwyx => new Vec4(w, w, y, x);
		public Vec4 wwyy => new Vec4(w, w, y, y);
		public Vec4 wwyz => new Vec4(w, w, y, z);
		public Vec4 wwyw => new Vec4(w, w, y, w);
		public Vec4 wwzx => new Vec4(w, w, z, x);
		public Vec4 wwzy => new Vec4(w, w, z, y);
		public Vec4 wwzz => new Vec4(w, w, z, z);
		public Vec4 wwzw => new Vec4(w, w, z, w);
		public Vec4 wwwx => new Vec4(w, w, w, x);
		public Vec4 wwwy => new Vec4(w, w, w, y);
		public Vec4 wwwz => new Vec4(w, w, w, z);
		public Vec4 wwww => new Vec4(w, w, w, w);
	}
}