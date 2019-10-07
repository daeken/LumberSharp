using System.Linq;
using System.Numerics;
using static MarchingBand.ShaderMath;
// ReSharper disable UnusedMember.Local

namespace MarchingBand {
	public static class Extensions {
		internal static string Indent(this string code, int level = 1) =>
			string.Join("\n", code.Split('\n').Select(x => new string('\t', level) + x));
	}
}