using System;
using System.Collections.Generic;
using System.Numerics;
using static Common.Helpers;

namespace Demarkate {
	public class BitGrid {
		readonly List<(int, int)> Region;
		readonly Vector2 A, B, C, D;
		readonly (int W, int H) Size;
		
		public BitGrid(List<(int, int)> region, (int, int) size, Vector2 a, Vector2 b, Vector2 c, Vector2 d) {
			Region = region;
			Size = size;
			A = a;
			B = b;
			C = c;
			D = d;
		}

		public bool this[int x, int y] {
			get {
				var xSpace = 1f / Size.W;
				var xOff = xSpace / 2;
				var ySpace = 1f / Size.H;
				var yOff = ySpace / 2;

				var tx = xSpace * x + xOff;
				var s1 = Mix(A, B, tx);
				var s2 = Mix(C, D, tx);
				var (px, py) = Mix(s1, s2, ySpace * y + yOff);
				return Region.Contains(((int) MathF.Round(px), (int) MathF.Round(py)));
			}
		}
	}
}