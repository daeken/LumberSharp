using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using ImageLib;
using PrettyPrinter;

namespace Converter {
	class Program {
		static (int, int)[] Neighbors = {
			(-1, -1), (0, -1), (1, -1), 
			(-1, 0), /*home*/ (1, 0), 
			(-1, 1), (0, 1), (1, 1)
		};
		
		static void Main(string[] args) {
			var cimg = Png.Decode("color", File.OpenRead("../Renderer/test.png"));
			var nimg = Png.Decode("normal", File.OpenRead("../Renderer/testn.png"));
			var dimg = Png.Decode("depth", File.OpenRead("../Renderer/testd.png"));
			var (width, height) = cimg.Size;

			var pixels = new Pixel[width * height];
			for(var i = 0; i < pixels.Length; ++i) {
				var color = new Vector4(cimg.Data[i * 4] / 255f, cimg.Data[i * 4 + 1] / 255f, cimg.Data[i * 4 + 2] / 255f, cimg.Data[i * 4 + 3] / 255f);
				var normal = new Vector3(nimg.Data[i * 3] / 255f, nimg.Data[i * 3 + 1] / 255f, nimg.Data[i * 3 + 2] / 255f);
				var depth = BitConverter.ToSingle(dimg.Data, i * 4);
				pixels[i] = new Pixel { Color = color, Normal = Vector3.Normalize(normal * new Vector3(2f) - new Vector3(1f)), Depth = depth };
			}

			Pixel sample(int x, int y) => x < 0 || x >= width || y < 0 || y >= height ? new Pixel() : pixels[y * width + x];

			IEnumerable<Pixel> sampleNeighbors(int x, int y) =>
				Neighbors.Select(t => sample(x + t.Item1, y + t.Item2))
					.Where(t => t.Normal.X != 0 || t.Normal.Y != 0 || t.Normal.Z != 0);

			"Loaded".Print();

			var maxDepthDelta = new float[pixels.Length];
			for(int y = 0, i = 0; y < height; ++y)
				for(var x = 0; x < width; ++x, ++i) {
					var pixel = pixels[i];
					if((pixel.Normal.X == 0 && pixel.Normal.Y == 0 && pixel.Normal.Z == 0) || float.IsInfinity(pixel.Depth)) continue;

					var neighborDeltas = sampleNeighbors(x, y).Select(n => MathF.Abs(pixel.Depth - n.Depth));
					maxDepthDelta[i] = neighborDeltas.Max();
					pixels[i].Edge = maxDepthDelta[i] > 0.9f;
				}

			var distanceField = new int[pixels.Length];
			for(int y = 0, i = 0; y < height; ++y)
				for(var x = 0; x < width; ++x, ++i) {
					var pixel = pixels[i];
					if(!pixel.Edge) continue;
					var box = 1;
					var dist = -1;
					while(dist == -1) {
						int dx;
						var dy = y - box;
						if(dy >= 0 && dy < height) {
							for(dx = x - box; dx <= x + box; ++dx) {
								if(dx < 0 || dx >= width || pixels[dy * width + dx].Edge) continue;
								dist = box;
								break;
							}
						}

						dy = y + box;
						if(dist == -1 && dy >= 0 && dy < height) {
							for(dx = x - box; dx <= x + box; ++dx) {
								if(dx < 0 || dx >= width || pixels[dy * width + dx].Edge) continue;
								dist = box;
								break;
							}
						}

						dx = x - box;
						if(dist == -1 && dx >= 0 && dx < width) {
							for(dy = y - box; dy <= y + box; ++dy) {
								if(dy < 0 || dy >= height || pixels[dy * width + dx].Edge) continue;
								dist = box;
								break;
							}
						}

						dx = x + box;
						if(dist == -1 && dx >= 0 && dx < width) {
							for(dy = y - box; dy <= y + box; ++dy) {
								if(dy < 0 || dy >= height || pixels[dy * width + dx].Edge) continue;
								dist = box;
								break;
							}
						}
						box++;
					}
					pixels[i].Distance = dist;
					Console.WriteLine($"{x} {y} {pixels[i].Depth} {pixels[i].Distance}");
				}

			/*var mdist = (float) distanceField.Max();
			var image = new Image(ColorMode.Greyscale, (width, height), distanceField.Select(p => (byte) (MathF.Round(p / mdist * 255))).ToArray());
			using(var fp = File.OpenWrite("angles.png"))
				Png.Encode(image, fp);*/
		}
	}
}