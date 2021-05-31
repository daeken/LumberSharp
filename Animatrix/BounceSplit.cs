using System.Collections.Generic;
using System.Numerics;
using static Common.Helpers;

namespace Animatrix {
	public class BounceSplit : IAnimation {
		public Vector2 Dimensions => new(1000, 1000);
		
		public List<List<(string, List<Vector2>)>> GenerateFrames() {
			var balls = 1;
			var radius = 100f;
			var frames = new List<List<(string, List<Vector2>)>>();
			for(var i = 0; i < 4; ++i) {
				var frame1 = new List<(string, List<Vector2>)>();
				var frame2 = new List<(string, List<Vector2>)>();
				var frame3 = new List<(string, List<Vector2>)>();
				var frame4 = new List<(string, List<Vector2>)>();
				var frame5 = new List<(string, List<Vector2>)>();
				var frame6 = new List<(string, List<Vector2>)>();
				var curOff = 1000f / (balls + 1);
				var nextOff = 1000f / (balls * 2 + 1);
				for(var j = 1; j <= balls; ++j) {
					frame1.Add(("black", Circle(new Vector2(curOff * j, 500), radius)));
					frame2.Add(("black", Circle(new Vector2(curOff * j, 750), radius)));
					frame3.Add(("black", Circle(new Vector2(curOff * j, 1000 - radius - 2), radius)));
					frame4.Add(("black", Circle(new Vector2(Mix(curOff * j, nextOff * j, 0.25f), 900 - radius / 2), radius / 2)));
					frame4.Add(("black", Circle(new Vector2(Mix(curOff * j, nextOff * (j + 1), 0.25f), 900 - radius / 2), radius / 2)));
					frame5.Add(("black", Circle(new Vector2(Mix(curOff * j, nextOff * j, 0.5f), 750), radius / 2)));
					frame5.Add(("black", Circle(new Vector2(Mix(curOff * j, nextOff * (j + 1), 0.5f), 750), radius / 2)));
					frame6.Add(("black", Circle(new Vector2(Mix(curOff * j, nextOff * j, 0.75f), 600), radius / 2)));
					frame6.Add(("black", Circle(new Vector2(Mix(curOff * j, nextOff * (j + 1), 0.75f), 600), radius / 2)));
				}
				radius /= 2;
				balls *= 2;
				frames.Add(frame1);
				frames.Add(frame2);
				frames.Add(frame3);
				frames.Add(frame4);
				frames.Add(frame5);
				frames.Add(frame6);
			}
			return frames;
		}
	}
}