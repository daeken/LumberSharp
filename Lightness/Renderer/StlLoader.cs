using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using MoonSharp.Interpreter;

namespace Lightness.Renderer {
	[MoonSharpUserData]
	public static class StlLoader {
		public static Model Load(string fn, bool recenter = false) {
			using(var fp = File.OpenRead(Path.Combine(Lightness.Program.BaseDirectory, fn))) {
				var data = new byte[fp.Length];
				fp.Read(data, 0, data.Length);
				var mesh = Encoding.ASCII.GetString(data, 0, 80).Contains("solid")
					? LoadText(Encoding.ASCII.GetString(data))
					: LoadBinary(data);
				if(recenter)
					mesh = Recenter(mesh);
				return new Model(mesh);
			}
		}

		static IReadOnlyList<Triangle> Recenter(IReadOnlyList<Triangle> mesh) {
			var low = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
			var high = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
			foreach(var tri in mesh) {
				low = Vector3.Min(tri.A, low);
				low = Vector3.Min(tri.B, low);
				low = Vector3.Min(tri.C, low);
				high = Vector3.Max(tri.A, high);
				high = Vector3.Max(tri.B, high);
				high = Vector3.Max(tri.C, high);
			}
			var extents = high - low;
			var center = extents / 2 + low;
			return mesh.Select(x => new Triangle(x.A - center, x.B - center, x.C - center, x.NA, x.NB, x.NC)).ToList();
		}

		static IReadOnlyList<Triangle> LoadBinary(byte[] data) {
			using(var br = new BinaryReader(new MemoryStream(data))) {
				br.ReadBytes(80);
				var numTris = br.ReadUInt32();
				var tris = new List<Triangle>();
				for(var i = 0; i < numTris; ++i) {
					var normal = ReadVec3(br);
					var a = ReadVec3(br);
					var b = ReadVec3(br);
					var c = ReadVec3(br);
					var attr = br.ReadUInt16();
					tris.Add(new Triangle(a, b, c, normal));
				}
				return tris;
			}
		}

		static IReadOnlyList<Triangle> LoadText(string data) {
			var triangles = new List<Triangle>();
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
							triangles.Add(new Triangle(verts[0], verts[1], verts[2], norm));
							off = 0;
						}

						break;
				}
			}
			return triangles;
		}
		
		static Vector3 ReadVec3(BinaryReader br) =>
			new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

		static Vector3 Parse(IEnumerable<string> elems, int offset) {
			var p = elems.Skip(offset).Take(3).Select(x => float.Parse(x, System.Globalization.NumberStyles.Any))
				.ToArray();
			return new Vector3(p[0], p[1], p[2]);
		}
	}
}