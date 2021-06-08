using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Common {
	public class Fiducial {
		public enum Side {
			TopAndBottom,
			LeftAndRight,
			Short,
			Long
		}
		
		public static List<Vector2> CreateFrame(Vector2 size, ushort data = 0, Side side = Side.Long) {
			side = side switch {
				Side.TopAndBottom => Side.TopAndBottom, 
				Side.LeftAndRight => Side.LeftAndRight, 
				Side.Short when size.Y > size.X => Side.TopAndBottom, 
				Side.Short => Side.LeftAndRight, 
				Side.Long when size.Y > size.X => Side.LeftAndRight, 
				_ => Side.TopAndBottom 
			};

			var gridSize = size.Apply(MathF.Min) / (8 + 9 * 3 + 2);

			return CreateMarker(gridSize, size, data).ToList();
		}

		static IEnumerable<Vector2> CreateMarker(float gridSize, Vector2 size, ulong data) {
			var corner = -size / 2;

			data |= 0b01_01_01_01_01_01_01_01__11_10_01_00_00_01_10_11UL << 16;

			for(var i = 0; i < 64; ++i) {
				var (x, y) = FromD(8, i);
				yield return corner + new Vector2(x * gridSize, -y * gridSize);
			}

			for(var i = 0; i < 48; i += 2) {
				var shift = corner + new Vector2(gridSize * (i + 8), 0);
				var scale = new Vector2(gridSize, -gridSize);
				foreach(var elem in Trit((int) ((data >> i) & 1), (int) ((data >> (i + 1)) & 1))) yield return elem * scale + shift;
			}

			yield return corner;

			for(var i = 0; i < 64; ++i) {
				var (x, y) = FromD(8, i);
				yield return corner + new Vector2(x * gridSize, -y * gridSize);
			}
			
			yield return new(-corner.X, corner.Y);
			
			for(var i = 0; i < 64; ++i) {
				var (x, y) = FromD(8, i);
				yield return -corner - new Vector2(x * gridSize, -y * gridSize);
			}

			data ^= (1UL << 50) - 1;
			for(var i = 0; i < 48; i += 2) {
				var shift = -corner - new Vector2(gridSize * (i + 8), 0);
				var scale = new Vector2(-gridSize, gridSize);
				foreach(var elem in Trit((int) ((data >> i) & 1), (int) ((data >> (i + 1)) & 1))) yield return elem * scale + shift;
			}

			yield return -corner;

			for(var i = 0; i < 64; ++i) {
				var (x, y) = FromD(8, i);
				yield return -corner - new Vector2(x * gridSize, -y * gridSize);
			}

			yield return new(corner.X, -corner.Y);

			yield return corner;
		}

		static IEnumerable<Vector2> Trit(int a, int b) {
			var v = ((a ^ b ^ 1) << 2) | (b << 1) | a;
			yield return new(0, 0);
			yield return new(0, v);
			yield return new(1, v);
			yield return new(1, 0);
		}
		
		static (int X, int Y) Rot((int X, int Y) p, int n, bool rx, bool ry) {
			if(ry) return p;
				
			var x = p.X;
			var y = p.Y;
			if(rx) {
				x = (n - 1) - x;
				y = (n - 1) - y;
			}

			return (y, x);
		}
			
		static (int X, int Y) FromD(int n, int d) {
			var p = (X: 0, Y: 0);
			var t = d;
 
			for(var s = 1; s < n; s <<= 1) {
				var rx = (t & 2) != 0;
				var ry = ((t ^ (rx ? 1 : 0)) & 1) != 0;
				p = Rot(p, s, rx, ry);
				p.X += rx ? s : 0;
				p.Y += ry ? s : 0;
				t >>= 2;
			}
				
			return p;
		}
	}
}