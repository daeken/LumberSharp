using System.Numerics;
using Facer;
using Common;
using DoubleSharp.Linq;
using DoubleSharp.MathPlus;
using static Common.Helpers;
using static System.MathF;

var model = Stl.Load("MothersDayStatueThinBase.stl", recenter: true, swapYZ: false);
Console.WriteLine($"Loaded model ({model.Count} triangles)");
var octree = new Octree(model, 50);
Console.WriteLine("Built octree");
//Stl.Save("test.stl", octree.Children[0].AllTriangles.ToList());

const float NearPlane = 0.01f;
const float FarPlane = 100;
var projection = Matrix4x4.CreatePerspectiveFieldOfView(Tau / 4, 1, NearPlane, FarPlane);
var cameraPos = new Vector3(0, 75, 10);
var view = Matrix4x4.CreateLookAt(cameraPos, new Vector3(0.0001f, 0.0001f, 0.0001f), -Vector3.UnitZ);
var viewProject = view * projection;
Vector2 Project(Vector3 point) {
	var p4 = Vector4.Transform(point, viewProject);
	if(p4.W == 0) return p4.XY();
	return new Vector2(p4.X, p4.Y) / p4.W;
}

bool CheckHidden(Vector3 p) {
	var dir = (cameraPos - p).Normalize();
	var origin = p + dir * 0.1f;
	return octree.Intersects(origin, dir);
}

IEnumerable<(Vector3 A, Vector3 B, Vector3 Normal)> RemoveHiddenSegments((Vector3 A, Vector3 B, Vector3 Normal) x, int depth = 0) {
	var (a, b, normal) = x;
	var ah = CheckHidden(a);
	var bh = CheckHidden(b);
	var m = (a + b) / 2;
	var mh = CheckHidden(m);
	if(!ah && !bh && !mh) {
		yield return (a, b, normal);
		yield break;
	}

	if(depth > 100 || (ah && bh && mh))
		yield break;

	if(!ah && !mh) {
		yield return (a, m, normal);
		foreach(var elem in RemoveHiddenSegments((m, b, normal), depth + 1))
			yield return elem;
	} else if(!mh && !bh) {
		foreach(var elem in RemoveHiddenSegments((a, m, normal), depth + 1))
			yield return elem;
		yield return (m, b, normal);
	} else if(!ah && !bh) {
		foreach(var elem in RemoveHiddenSegments((a, m, normal), depth + 1))
			yield return elem;
		foreach(var elem in RemoveHiddenSegments((m, b, normal), depth + 1))
			yield return elem;
	}
}

IEnumerable<(Vector3 A, Vector3 B, Vector3 Normal)> Subdivide((Vector3 A, Vector3 B, Vector3 Normal) x) {
	var (a, b, normal) = x;
	var count = 10;
	var step = 1f / count;
	for(var i = 0; i < count; ++i)
		yield return (Mix(a, b, i * step), Mix(a, b, (i + 1) * step), normal);
}

IEnumerable<(Vector3 A, Vector3 B, Vector3 Normal)> CombineSegments(List<(Vector3 A, Vector3 B, Vector3 Normal)> segments) {
	if(segments.Count == 0)
		yield break;
	if(segments.Count == 1) {
		yield return segments[0];
		yield break;
	}

	var (a, b, normal) = segments[0];
	foreach(var (na, nb, nn) in segments.Skip(1)) {
		if(na != b || Abs(Vector3.Dot(nn, normal)) > 0.01f) {
			yield return (a, b, normal);
			(a, b, normal) = (na, nb, nn);
		} else
			b = nb;
	}
	yield return (a, b, normal);
}

var e = Max(octree.Size.X, Max(octree.Size.Y, octree.Size.Z)) * 0.1f;
Visualizer.Run(() => {
	var slices = 250;
	var allPaths = Enumerable.Range(0, slices).AsParallel().Select(i => {
		Console.WriteLine($"Doing slice {i}");
		var rng = new Random();
		var slicePlane = new Vector3(0, -0.2f, 1).Normalize();
		//var slicePlane = new Vector3(rng.Next(3) != 0 ? 0.35f : -0.35f, i % 2 == 0 ? -0.1f : 0.1f, 1).Normalize();
		var bottom = Vector3.Dot(octree.Min, slicePlane.Abs()) - e;
		var top = Vector3.Dot(octree.Max, slicePlane.Abs()) + e;
		var sliceDistance = (top - bottom) / (slices - 1);
		var segments = CombineSegments(octree.FindPlaneIntersections(cameraPos, slicePlane, sliceDistance * i + bottom)
				.Select(Subdivide).SelectMany(x => x)
				.Select(RemoveHiddenSegments).SelectMany(x => x)
				.ToList()
			)
			//.Where(x => Abs(Acos(Vector3.Dot((cameraPos - (x.A + x.B) / 2).Normalize(), x.Normal))) / PI * 2 > rng.NextSingle())
			.Select(x => (x.A, x.B).AsEnumerable().ToList()).ToList();
		Console.WriteLine($"Sliced! {segments.Count} points. Joining paths");
		if(segments.Count == 0)
			return Enumerable.Empty<List<Vector2>>();
		var paths = segments.Select(x => x.Select(Project).ToList()).ToList();
		paths = paths.ReorderPaths();
		paths = paths.TriviallyJoinPaths();
		paths = paths.ReorderPaths();
		//paths = paths.SimplifyPaths(0.01f);
		Console.WriteLine($"Total paths for slice: {paths.Count}");
		Visualizer.DrawPaths(paths);
		return paths;
	}).SelectMany(x => x).ToList();
	Console.WriteLine($"All slicing done! {allPaths.Count} paths");
	allPaths = allPaths.ReorderPaths();
	Console.WriteLine("Finished reordering");
	//Visualizer.DrawPaths(allPaths);
	Visualizer.WaitForInput();
	SvgHelper.Output("test.svg", allPaths, new());
});