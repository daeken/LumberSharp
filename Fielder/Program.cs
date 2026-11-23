using System.Numerics;
using Common;
using DotnetNoise;

var noise = new FastNoise { UsedNoiseType = FastNoise.NoiseType.Perlin };

var h = 0.001f;
var (hx, hy) = (new Vector2(h, 0), new Vector2(0, h));
float Field(Vector2 v) => noise.GetNoise(v.X, v.Y);
Vector2 Gradient(Vector2 v) => new Vector2(
	Field(v + hx) - Field(v - hx),
	Field(v + hy) - Field(v - hy)
) / h;

Visualizer.Run(() => {
	var size = new Vector2(1000, 1000);
	const int pathCount = 5000;
	const int stepsPerPath = 10000;
	var rngs = Enumerable.Range(0, pathCount).Select(_ => new Random()).ToList();
	var allPaths = rngs.AsParallel().Select(rng => {
		var dir = rng.Next(2) == 0 ? 1 : -1;
		var p = new Vector2(rng.NextSingle(), rng.NextSingle()) * size;
		List<Vector2> path = [with(capacity: stepsPerPath + 1), p];
		for(var i = 0; i < stepsPerPath; ++i) {
			var g = Gradient(p);
			p += g * dir;
			path.Add(p);
		}
		path = path.SimplifyPath(0.1f);
		Visualizer.DrawPath(path);
		Console.WriteLine(path.Count);
		return path;
	}).ToList();
	Visualizer.WaitForInput();
	allPaths = allPaths.ReorderPaths();
	allPaths = allPaths.JoinPaths();
	allPaths = allPaths.ReorderPaths();
	SvgHelper.Output("test.svg", allPaths, new());
});
