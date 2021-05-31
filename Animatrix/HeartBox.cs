using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Common;
using static Common.Helpers;

namespace Animatrix {
	public class HeartBox : IAnimation {
		public Vector2 Dimensions => new(1000, 1000);
		public List<List<(string, List<Vector2>)>> GenerateFrames() {
			var heart = SvgHelper.PathsFromSvg("heart-outline.svg").Select(x => x.Points).ToList();
			heart = SvgHelper.Fit(heart, Vector2.One);
			
			var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.Tau / 3, 1, 1f, 1000f);
			Console.WriteLine(projection);
			var view = Matrix4x4.CreateLookAt(new Vector3(0, 0, -5f), new Vector3(0.01f, 0.01f, 1f), Vector3.UnitY);
			
			var frames = new List<List<(string, List<Vector2>)>>();
			var numFrames = 30;
			var step = MathF.Tau / (numFrames - 1);
			for(var i = 0; i < numFrames; ++i) {
				//var mm = Matrix4x4.CreateFromAxisAngle(new Vector3(.25f, .4f, 0f), i * step);
				var mm = Matrix4x4.CreateRotationY(i * step * 2) * Matrix4x4.CreateRotationX(i * step);
				var m = mm * projection;

				Vector2 P(Vector3 p) {
					var tp = Vector4.Transform(p, m);
					return new Vector2(tp.X + 500, tp.Y + 500);
				}

				var frame = new List<(string, List<Vector2>)>();

				var corners = new Vector3[] {
					new(-1, -1, -1), new(-1, -1,  1), 
					new(-1,  1, -1), new(-1,  1,  1),
					new( 1, -1, -1), new( 1, -1,  1), 
					new( 1,  1, -1), new( 1,  1,  1)
				}.Select(x => x * 450).ToArray();
				var edges = new[] {
					(0, 1), (1, 5), (5, 4), (4, 0), 
					(0, 2), (1, 3), (5, 7), (4, 6), 
					(2, 3), (3, 7), (7, 6), (6, 2)
				};
				var faces = new[] {
					(0, 2, 6, 4), 
					(1, 3, 7, 5), 
					(0, 1, 3, 2), 
					(4, 5, 7, 6), 
					(2, 3, 7, 6), 
					(0, 1, 5, 4)
				};
				foreach(var (a, b) in edges)
					frame.Add(("black", new List<Vector2> { P(corners[a]), P(corners[b]) }));
				foreach(var (a, b, c, d) in faces) {
					var pa = P(corners[a]);
					var pb = P(corners[b]);
					var pc = P(corners[c]);
					var pd = P(corners[d]);

					Vector2 T(Vector2 p) {
						var s1 = Mix(pa, pb, p.Y);
						var s2 = Mix(pd, pc, p.Y);
						return Mix(s1, s2, p.X);
					}
					
					frame.AddRange(heart.Select(path => ("pink", path.Select(T).ToList())));
				}
				
				frames.Add(frame);
			}
			return frames;
		}
	}
}