using System.Numerics;
using Common;
using DoubleSharp.MathPlus;
using SdfLib;
using static SdfLib.Sdf3D;
using static System.MathF;
using static System.Numerics.Vector3;
using static Common.Helpers;

var NearPlane = 1;
var FarPlane = 1000;
var Projection = Matrix4x4.CreatePerspectiveFieldOfView(Tau / 4, 1, NearPlane, FarPlane);
var CameraPos = new Vector3(0, 0, -5).RotateY(Tau / 360 * -30/*45*/);//.RotateZ(Tau / 360 * 36);
var View = Matrix4x4.CreateLookAt(CameraPos, new Vector3(0.0001f, 0.0001f, 0.0001f), UnitX);
var ViewProject = View * Projection;

float Scene(Vector3 p) {
	return Dodecahedron(p, 1, 0);
	//return Sdf3D.Sphere(p + Vector3.UnitX * 300 - Vector3.UnitY * 10, 15);
	return Subtract(
		Box(p, One),
		Sphere(p, 1.2f)
	);
}

Visualizer.Run(() => {
	Main();
	Visualizer.WaitForInput();
});

List<Vector3> FindCuttingPlanes() {
	var (origin, radius) = FindBoundingSphere(Scene);
	radius *= 1.5f;
	var flatAt = new Dictionary<Vector3, List<Vector3>>();
	var samplePoints = new[] {
		Vector2.UnitX,
		-Vector2.UnitX,
		Vector2.UnitY,
		-Vector2.UnitY,
	}.Select(x => x * 0.01f).ToList();
	foreach(var p in EvenlySampleSphere(origin, radius, pointsPerAxis: 180)) {
		var t = March(Scene, p, origin);
		if(t == 1) continue;
		var hit = Mix(p, origin, t);
		var normal = FirstDerivative(Scene, hit);
		var xAxis = (normal.Abs() - UnitX).LengthSquared() > Epsilon
			? UnitX
			: (normal.Abs() - UnitZ).LengthSquared() > Epsilon
				? UnitZ
				: UnitY;
		xAxis = Normalize(Cross(normal, xAxis));
		var yAxis = Normalize(Cross(normal, xAxis));
		var flatness = samplePoints.Select(v => v.X * xAxis + v.Y * yAxis + hit).Select(Scene).Select(Abs).Max();
		if(flatness > Epsilon)
			continue;
		normal = normal.Apply(v => Round(v, 2)).Normalize();
		if(!flatAt.TryGetValue(normal, out var bucket) && !flatAt.TryGetValue(-normal, out bucket))
			bucket = flatAt[normal] = new();
		bucket.Add(hit);
	}
	Console.WriteLine($"Found {flatAt.Count} cutting planes!");
	foreach(var plane in flatAt)
		Console.WriteLine($"\t- {plane.Key} -- {plane.Value.Count}");
	return flatAt.OrderByDescending(x => x.Value.Count).Select(x => x.Key).Take(6).ToList();
}

void Main() {
	//FindCuttingPlanes();
	/*var axes = new[] {
		UnitX, 
		UnitY, 
		UnitZ
	};*/
	var axes = FindCuttingPlanes();

	var dist = 0.989f; //0.93427896f;
	
	var paths = axes.Select(axis => Enumerable.Range(-5, 11).Select(x => x / 5f * dist).Select(d => (axis, d)))
		.SelectMany(x => x)
		.AsParallel().Select(x => {
			var (scene2d, updimension) = Slice(Scene, x.axis, x.d);
			var spaths = new SdfQuadtree(scene2d).FindPaths();
			//spaths = spaths.Apply(p => Project(updimension(p)));
			//Visualizer.DrawPaths(spaths);
			var tpaths = spaths.Apply(updimension);
			tpaths = tpaths.Select(path => Subdivide(path).ToList()).ToList();
			tpaths = tpaths.Select(RemoveHiddenSegments).SelectMany(y => y).ToList();
			spaths = tpaths.Apply(Project);
			spaths = spaths.SimplifyPaths(0.001f);
			Visualizer.DrawPaths(spaths);
			return spaths;
		}).SelectMany(x => x).ToList();
	paths = paths.ReorderPaths().TriviallyJoinPaths();
	paths = paths.ReorderPaths().TriviallyJoinPaths();
	SvgHelper.Output("test.svg", paths, new());
}

Vector2 Project(Vector3 point) {
	var p4 = Vector4.Transform(point, ViewProject);
	return new Vector2(p4.X, p4.Y) / p4.W * 10;
}

static float March(Func<Vector3, float> f, Vector3 from, Vector3 to, int steps = 256) {
	var rd = Normalize(to - from);
	var t = 0f;
	for(var i = 0; i < steps; ++i) {
		var p = from + rd * t;
		var d = f(p);
		if(Abs(d) <= Epsilon)
			return t / (to - from).Length();
		t += d * 0.9f;
	}
	return 1;
}

bool IsObscured(Vector3 p) {
	p = FindClosestSurfacePoint(Scene, p);
	var normal = FirstDerivative(Scene, p);
	var rd = Normalize(p - CameraPos);
	return March(Scene, CameraPos, p) < 0.999f;
}

Vector3 FindLastObscured(Vector3 unobscured, Vector3 obscured) {
	var su = unobscured;
	var so = obscured;
	while((unobscured - obscured).LengthSquared() > .00001f) {
		var mp = Mix(unobscured, obscured, .5f);
		if(IsObscured(mp))
			obscured = mp;
		else
			unobscured = mp;
	}
	return obscured;
}

List<List<Vector3>> RemoveHiddenSegments(List<Vector3> path) {
	var opath = path.Select(p => (p, IsObscured(p))).ToList();
	var paths = new List<List<Vector3>> { new() };
	Vector3? lpoint = null;
	var lastObscured = false;
	var cpath = paths[0];
	foreach(var (p, obscured) in opath) {
		if(obscured) {
			if(lpoint == null || lastObscured) {
				lpoint = p;
				lastObscured = true;
				continue;
			}
			lpoint = FindLastObscured(lpoint.Value, p);
			lastObscured = true;
			cpath.Add(lpoint.Value);
			paths.Add(cpath = new());
			continue;
		}

		if(lastObscured)
			cpath.Add(FindLastObscured(p, lpoint.Value));
		lpoint = p;
		cpath.Add(p);
		lastObscured = false;
	}
	return paths.Where(p => p.Count > 1).ToList();
}

IEnumerable<Vector3> Subdivide(List<Vector3> path) {
	if(path.Count <= 1) yield break;

	var res = 0.0005f;
			
	var lp = FindClosestSurfacePoint(Scene, path.First());
	yield return lp;
	foreach(var _np in path.Skip(1)) {
		var np = FindClosestSurfacePoint(Scene, _np);
		var d = np - lp;
		var steps = (int) Ceiling(d.Length() / res);
		var chunk = d / (steps + 1);
		for(var i = 0; i < steps - 1; ++i)
			yield return FindClosestSurfacePoint(Scene, lp + chunk * i);
		yield return np;
		lp = np;
	}
}
