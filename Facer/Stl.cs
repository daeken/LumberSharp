using DoubleSharp.IO;

namespace Facer;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

public static class Stl {
	public static IReadOnlyList<Triangle3D> Load(string fn, bool recenter = false, bool swapYZ = false) {
		var data = File.ReadAllBytes(fn);
		var mesh = Encoding.ASCII.GetString(data, 0, 80).Contains("solid") && data.All(x => x != 0)
			? LoadText(Encoding.ASCII.GetString(data))
			: LoadBinary(data);
		mesh = swapYZ ? mesh.Select(x => x.SwapYZ()).ToList() : mesh;
		return recenter ? Recenter(mesh) : mesh;
	}

	public static void Save(string fn, IReadOnlyList<Triangle3D> triangles) {
		using var fp = File.Open(fn, FileMode.Create);
		using var sw = new StreamWriter(fp);
		sw.WriteLine("solid foo");
		foreach(var tri in triangles) {
			sw.WriteLine($"facet normal {tri.Normal.X} {tri.Normal.Y} {tri.Normal.Z}");
			sw.WriteLine("outer loop");
			sw.WriteLine($"vertex {tri.A.X} {tri.A.Y} {tri.A.Z}");
			sw.WriteLine($"vertex {tri.B.X} {tri.B.Y} {tri.B.Z}");
			sw.WriteLine($"vertex {tri.C.X} {tri.C.Y} {tri.C.Z}");
			sw.WriteLine("endloop");
			sw.WriteLine("endfacet");
		}
		sw.WriteLine("endsolid foo");
	}

	static IReadOnlyList<Triangle3D> Recenter(IReadOnlyList<Triangle3D> mesh) {
		var low = mesh.Select(x => x.Points).SelectMany(x => x).Aggregate(Vector3.Min);
		var high = mesh.Select(x => x.Points).SelectMany(x => x).Aggregate(Vector3.Max);
		var extents = high - low;
		var center = extents / 2 + low;
		return mesh.Select(x => new Triangle3D(x.A - center, x.B - center, x.C - center/*, x.NA, x.NB, x.NC*/)).ToList();
	}

	static IReadOnlyList<Triangle3D> LoadBinary(byte[] data) {
		using var br = new BinaryReader(new MemoryStream(data));
		br.ReadBytes(80);
		var numTris = br.ReadUInt32();
		var tris = new List<Triangle3D>();
		for(var i = 0; i < numTris; ++i) {
			var normal = br.ReadVector3();
			var a = br.ReadVector3();
			var b = br.ReadVector3();
			var c = br.ReadVector3();
			var attr = br.ReadUInt16();
			tris.Add(new(a, b, c));
		}
		return tris;
	}

	static IReadOnlyList<Triangle3D> LoadText(string data) {
		var triangles = new List<Triangle3D>();
		var verts = new Vector3[3];
		var off = 0;
		var norm = new Vector3(0, 0, 1);
		foreach(var line in data.Split('\n').Select(x => x.Trim())) {
			var elems = line.Split(' ');
			switch(elems[0]) {
				case "facet":
					norm = Parse(elems, 2);
					break;
				case "vertex":
					verts[off++] = Parse(elems, 1);
					if(off == 3) {
						triangles.Add(new(verts[0], verts[1], verts[2]));
						off = 0;
					}

					break;
			}
		}
		return triangles;
	}
	
	static Vector3 Parse(IEnumerable<string> elems, int offset) {
		var p = elems.Skip(offset).Take(3).Select(x => float.Parse(x, System.Globalization.NumberStyles.Any))
			.ToArray();
		return new(p[0], p[1], p[2]);
	}
}
