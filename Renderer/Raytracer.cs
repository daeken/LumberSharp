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

			var (tri, mat, dist) = hit.Value;
			var pos = ray.Origin + ray.Direction * dist;
			var uv = CalcUV(tri, ray);
			var normal = ((1f - uv.X - uv.Y) * tri.NA + uv.X * tri.NB + uv.Y * tri.NC).Normalized();

			var surfaceColor = GetColor(mat, pos, normal);

			if(mat.Reflectivity > 0) {
				var rdir = (ray.Direction - 2f * Vector3.Dot(ray.Direction, normal) * normal).Normalized();
				var reflection = CalcPoint(new Ray { Origin = pos + normal * 0.01f, Direction = rdir }, depth + 1);
				if(reflection != null)
					surfaceColor += reflection.Value.Color * mat.Reflectivity;
			}
			
			return new Pixel { Color = surfaceColor, Depth = dist, Normal = normal, Position = pos };
		}

		Vector3 GetColor(Material mat, Vector3 pos, Vector3 normal) {
			var color = Scene.AmbientColor * mat.Albedo;
			foreach(var light in Scene.Lights)
				color += min(max(CalcLightContribution(pos, normal, light), Vector3.Zero), Vector3.One) * mat.Albedo;
			return color;
		}
		
		Vector3 CalcLightContribution(Vector3 pos, Vector3 normal, Light light) {
			switch(light) {
				case DirectionalLight dl:
					return dl.Color * Vector3.Dot(normal, -dl.Direction);
				default: throw new NotImplementedException();
			}
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

		(Triangle, Material, float)? FindHit(Ray ray) {
			Triangle? closest = null;
			Material closestMat = null;
			var dist = float.PositiveInfinity;
			foreach(var octree in Scene.Octrees) {
				var mi = octree.FindIntersectionCustom(ray);
				if(mi == null || mi.Value.Item4 > dist) continue;
				closest = mi.Value.Item1;
				closestMat = mi.Value.Item2;
				dist = mi.Value.Item4;
			}
			if(closest == null) return null;
			return (closest.Value, closestMat, dist);
		}
	}
}