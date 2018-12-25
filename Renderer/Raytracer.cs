using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using PrettyPrinter;
using static Renderer.Extensions;

namespace Renderer {
	public class Raytracer {
		const int MaxDepth = 10;
		
		readonly Scene Scene;
		readonly Camera Camera;
		
		public Raytracer(Scene scene, Camera camera) {
			Scene = scene;
			Camera = camera;
		}

		public Pixel[] Render(int width, int height) {
			var pixels = new Pixel[width * height];
			for(var i = 0; i < width * height; ++i)
				pixels[i].Depth = float.PositiveInfinity;

			var xStep = 2f / width;
			var yStep = 2f / height;
			var xOff = -1f + xStep / 2;
			var yOff = -1f + yStep / 2;

			var aspectRatio = (float) width / height;

			Parallel.For(0, height, y => {
				var yr = yOff + y * yStep;
				for(var x = 0; x < width; ++x) {
					var i = y * width + x;
					var ray = Camera.GenerateRay(xOff + x * xStep, -yr, aspectRatio);
					var pixel = CalcPoint(ray, 0);
					if(pixel != null)
						pixels[i] = pixel.Value;
				}
			});

			return pixels;
		}

		Pixel? CalcPoint(Ray ray, int depth) {
			if(depth >= MaxDepth) return null;
			var hit = FindHit(ray);
			if(hit == null) return null;

			var (tri, dist) = hit.Value;
			var pos = ray.Origin + ray.Direction * dist;
			var uv = CalcUV(tri, ray);
			var normal = ((1f - uv.X - uv.Y) * tri.NA + uv.X * tri.NB + uv.Y * tri.NC).Normalized();

			return new Pixel { Depth = dist, Normal = normal, Position = pos };
		}

		Vector2 CalcUV(Triangle tri, Ray ray) {
			var edge1 = tri.B - tri.A;
			var edge2 = tri.C - tri.A;
			var h = Vector3.Cross(ray.Direction, edge2);
			var a = Vector3.Dot(edge1, h);
			var f = 1 / a;
			var s = ray.Origin - tri.A;
			var u = f * Vector3.Dot(s, h);
			var q = Vector3.Cross(s, edge1);
			var v = f * Vector3.Dot(ray.Direction, q);
			return new Vector2(u, v);
		}

		(Triangle, float)? FindHit(Ray ray) {
			Triangle? closest = null;
			var dist = float.PositiveInfinity;
			foreach(var octree in Scene.Octrees) {
				var mi = octree.FindIntersectionCustom(ray);
				if(mi == null || mi.Value.Item3 > dist) continue;
				closest = mi.Value.Item1;
				dist = mi.Value.Item3;
			}
			if(closest == null) return null;
			return (closest.Value, dist);
		}
	}
}