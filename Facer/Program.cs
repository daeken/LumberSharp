using System.Numerics;
using Facer;
using Common;
using DoubleSharp.Linq;
using DoubleSharp.MathPlus;
using static Common.Helpers;
using static System.MathF;

var cameraPos = new Vector3(0, 20, 0);

var model = Stl.Load("uploads_files_898133_medusa+500k.stl", recenter: true, swapYZ: true);
//model = model.Select(x => x.Transform(y => y * 5)).ToList();
Console.WriteLine($"Loaded model ({model.Count} triangles)");
model = model.AsParallel()
	.Where(tri => 
		Vector3.Dot(tri.Normal, (cameraPos - tri.Centroid).Normalize()) >= 0).ToList();
Console.WriteLine($"Culled backfaces ({model.Count} triangles)");
var octree = new Octree(model, 50);
Console.WriteLine("Built octree");
//Stl.Save("test.stl", octree.Children[0].AllTriangles.ToList());

const float NearPlane = 0.01f;
const float FarPlane = 100;
var projection = Matrix4x4.CreatePerspectiveFieldOfView(Tau / 4, 1, NearPlane, FarPlane);
var view = Matrix4x4.CreateLookAt(cameraPos, new Vector3(0.0001f, 0.0001f, 0.0001f), -Vector3.UnitZ);
var viewProject = view * projection;
Vector2 Project(Vector3 point) {
	var p4 = Vector4.Transform(point, viewProject);
	if(p4.W == 0) return p4.XY();
	return new Vector2(p4.X, p4.Y) / p4.W;
}

bool CheckHidden(Vector3 p, Triangle3D tri) {
	var dir = (cameraPos - p).Normalize();
	var origin = p;
	return octree.Intersects(origin, dir, except: tri);
}

IEnumerable<(Vector3 A, Vector3 B, Triangle3D Tri)> RemoveHiddenSegments((Vector3 A, Vector3 B, Triangle3D Tri) x, int depth = 0) {
	var (a, b, tri) = x;
	var ah = CheckHidden(a, tri);
	var bh = CheckHidden(b, tri);
	var m = (a + b) / 2;
	var mh = CheckHidden(m, tri);
	if(!ah && !bh && !mh) {
		yield return (a, b, tri);
		yield break;
	}

	if(depth > 100 || (ah && bh && mh))
		yield break;

	if(!ah && !mh) {
		yield return (a, m, tri);
		foreach(var elem in RemoveHiddenSegments((m, b, tri), depth + 1))
			yield return elem;
	} else if(!mh && !bh) {
		foreach(var elem in RemoveHiddenSegments((a, m, tri), depth + 1))
			yield return elem;
		yield return (m, b, tri);
	} else if(!ah && !bh) {
		foreach(var elem in RemoveHiddenSegments((a, m, tri), depth + 1))
			yield return elem;
		foreach(var elem in RemoveHiddenSegments((m, b, tri), depth + 1))
			yield return elem;
	}
}

IEnumerable<(Vector3 A, Vector3 B, Triangle3D Tri)> RemoveHiddenSegmentsTri(
	(Vector3 A, Vector3 B, Triangle3D Tri) x
) {
	var (a, b, tri) = x;
	var segments = new List<(Vector3 A, Vector3 B)> { (a, b) };
	var nt = new Triangle3D(a, b, cameraPos);
	var abl = (b - a).Length();
	var abn = (b - a).Normalize();
	foreach(var itri in octree.Intersects(nt, except: tri)) {
		segments = segments.Select(itri.ClipSegment).SelectMany(v => v).ToList();
		if(segments.Count == 0)
			break;
	}
	return segments.Select(v => (v.A, v.B, tri));
}

IEnumerable<(Vector3 A, Vector3 B, Triangle3D Tri)> Subdivide((Vector3 A, Vector3 B, Triangle3D Tri) x) {
	var (a, b, normal) = x;
	var count = 10;
	var step = 1f / count;
	for(var i = 0; i < count; ++i)
		yield return (Mix(a, b, i * step), Mix(a, b, (i + 1) * step), normal);
}

IEnumerable<(Vector3 A, Vector3 B, Triangle3D Tri)> CombineSegments(List<(Vector3 A, Vector3 B, Triangle3D Tri)> segments) {
	if(segments.Count == 0)
		yield break;
	if(segments.Count == 1) {
		yield return segments[0];
		yield break;
	}

	var (a, b, tri) = segments[0];
	foreach(var (na, nb, nt) in segments.Skip(1)) {
		if(na != b || tri != nt) {
			yield return (a, b, tri);
			(a, b, tri) = (na, nb, nt);
		} else
			b = nb;
	}
	yield return (a, b, tri);
}

static List<List<Vector3>> TriviallyJoinPaths(List<List<Vector3>> fpaths) {
	var paths = fpaths.Select(x => x.Select(y => new Vector3I(y)).ToList()).ToList();
	var mp = new Dictionary<Vector3I, List<Vector3I>>();

	foreach(var path in paths) {
		var start = path[0];
		var end = path[^1];
		if(mp.TryGetValue(start, out var capath)) {
            var cstart = capath[0];
			if(cstart == start) {
				path.Reverse();
				capath.InsertRange(0, path.SkipLast(1));
				mp.Remove(start);
				mp[end] = capath;
			} else {
				capath.AddRange(path.Skip(1));
				mp.Remove(start);
				mp[end] = capath;
			}
		} else if(mp.TryGetValue(end, out var cpath)) {
            var cstart = cpath[0];
			if(cstart == end) {
				cpath.InsertRange(0, path.SkipLast(1));
				mp.Remove(end);
				mp[start] = cpath;
			} else {
				path.Reverse();
				cpath.AddRange(path.Skip(1));
				mp.Remove(end);
				mp[start] = cpath;
			}
		} else {
			mp[start] = path;
			mp[end] = path;
		}
	}

	var npaths = new List<List<Vector3I>>();
	foreach(var v in mp.Values)
		if(!npaths.Contains(v))
			npaths.Add(v);
	//npaths = npaths.Select(x => x.Select(y => y / 100).ToList()).ToList();
	return npaths.Select(x => x.Select(y => y.Vector3).ToList()).ToList();
}

List<List<Vector3>> RemoveOverlaps(List<List<Vector3>> segments, Vector3 normal, float distance) {
	var eps = 0.001f;
	var xAxis = (normal.Abs() - Vector3.UnitX).LengthSquared() > eps
		? Vector3.UnitX
		: (normal.Abs() - Vector3.UnitZ).LengthSquared() > eps
			? Vector3.UnitZ
			: Vector3.UnitY;
	xAxis = Vector3.Cross(normal, xAxis).Normalize();
	var yAxis = Vector3.Cross(normal, xAxis).Normalize();
	var r = normal * -distance;

	Vector2 Project(Vector3 p) =>
		new(
			Vector3.Dot(xAxis, p - r),
			Vector3.Dot(yAxis, p - r)
		);

	Vector3 Unproject(Vector2 p) =>
		r + xAxis * p.X + yAxis * p.Y;

	var paths = segments.Select(x => x.Select(Project).ToList()).ToList();
	paths = paths.RemoveOverlaps(0.1f);
	paths = paths.SimplifyPaths(0.0001f);
	return paths.Select(x => x.Select(Unproject).ToList()).ToList();
}

var e = Max(octree.Size.X, Max(octree.Size.Y, octree.Size.Z)) * 0.1f;
Visualizer.Run(() => {
	var slices = 500;
	var allPaths = Enumerable.Range(0, slices).AsParallel().Select(i => {
		Console.WriteLine($"Doing slice {i}");
		var rng = new Random();
		var slicePlane = new Vector3(0.2f, -0.1f, 1).Normalize();
		//var slicePlane = new Vector3(rng.Next(3) != 0 ? 0.35f : -0.35f, i % 2 == 0 ? -0.1f : 0.1f, 1).Normalize();
		var bottom = Vector3.Dot(octree.Min, slicePlane.Abs()) - e;
		var top = Vector3.Dot(octree.Max, slicePlane.Abs()) + e;
		var sliceDistance = (top - bottom) / (slices - 1);
		var curDistance = sliceDistance * i + bottom;
		/*var segments = CombineSegments(octree.FindPlaneIntersections(slicePlane, curDistance)
				.Select(Subdivide).SelectMany(x => x)
				.Select(RemoveHiddenSegments).SelectMany(x => x)
				.ToList()
			)
			.Select(x => (x.A, x.B).AsEnumerable().ToList()).ToList();*/
		var segments = octree.FindPlaneIntersections(slicePlane, curDistance)
			.Select(RemoveHiddenSegmentsTri).SelectMany(x => x)
			.Select(x => (x.A, x.B).AsEnumerable().ToList()).ToList();
		var count = segments.Count;
		Console.WriteLine($"Sliced! {count} points. Joining paths in 3d");
		segments = RemoveOverlaps(segments, slicePlane, curDistance);
		Console.WriteLine($"Reduced {count} to {segments.Count}");
		if(segments.Count == 0)
			return Enumerable.Empty<List<Vector2>>();
		var paths = segments.Select(x => x.Select(Project).ToList()).ToList();
		paths = paths.Where(x => x.CalcDrawDistance() > 0.001f).ToList();
		paths = paths.ReorderPaths();
		paths = paths.TriviallyJoinPaths();
		paths = paths.ReorderPaths();
		var beforeDist = paths.CalcDrawDistance();
		//paths = paths.RemoveOverlaps();
		paths = paths.SimplifyCollinear();
		paths = paths.SimplifyPaths(0.0001f);
		Console.WriteLine($"Total paths for slice: {paths.Count} -- {paths.CalcDrawDistance()} vs {beforeDist}");
		Visualizer.DrawPaths(paths);
		return paths;
	}).SelectMany(x => x).ToList();
	Console.WriteLine($"All slicing done! {allPaths.Count} paths");
	/*allPaths = allPaths.RemoveOverlaps();
	Console.WriteLine($"After dedupe: {allPaths.Count}");*/
	allPaths = allPaths.Where(x => x.CalcDrawDistance() > 0.001f).ToList();
	Console.WriteLine($"After removing shorts: {allPaths.Count}");
	//Visualizer.DrawPaths(allPaths);
	Visualizer.WaitForInput();
	SvgHelper.Output("test.svg", allPaths, new());
});