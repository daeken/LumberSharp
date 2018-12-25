using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Lightness.Renderer {
	public static class StlLoader {
		public static IReadOnlyList<Triangle> Load(string data) {
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

		static Vector3 Parse(IEnumerable<string> elems, int offset) {
			var p = elems.Skip(offset).Take(3).Select(x => float.Parse(x, System.Globalization.NumberStyles.Any))
				.ToArray();
			return new Vector3(p[0], p[1], p[2]);
		}
	}
}