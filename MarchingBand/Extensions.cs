using System.Linq;

namespace MarchingBand {
	public static class Extensions {
		public static string Indent(this string code, int level = 1) =>
			string.Join("\n", code.Split('\n').Select(x => new string('\t', level) + x));
	}
}