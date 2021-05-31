using System;
using MoreLinq;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Common;

namespace Animatrix {
	class Program {
		static void Main(string[] args) {
			var leftPad = 0.5f;
			var slide = 0.02f;
			var between = new Vector2(100, 100);

			IAnimation animation = new HeartBox();
			var frames = animation.GenerateFrames();
			var paths = new List<(string Color, List<Vector2> Points)>();
			var perRow = (int) MathF.Round(MathF.Sqrt(frames.Count));

			var maxFrameSize = new Vector2(leftPad + slide * frames.Count + 1, 1) * animation.Dimensions;
			
			frames.ForEach((f, i) => {
				var suffix = $"###frame{i}";
				var fx = i % perRow;
				var fy = i / perRow;

				var corner = (maxFrameSize + between) * new Vector2(fx, fy);
				var frameSize = new Vector2(leftPad + slide * i + 1, 1) * animation.Dimensions;
				var frameOff = frameSize - animation.Dimensions;
				var coff = corner + frameOff;
				
				paths.Add(("green" + suffix, new List<Vector2> {
					corner, 
					new(corner.X + frameSize.X, corner.Y), 
					new(corner.X + frameSize.X, corner.Y + frameSize.Y), 
					new(corner.X, corner.Y + frameSize.Y), 
					corner
				}));
				paths.AddRange(f.Select(x => (x.Item1 + suffix, x.Item2.Select(y => y + coff).ToList())));
			});
			
			SvgHelper.Output(args[0], paths, new Page());
		}
	}
}