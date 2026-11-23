using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Cairo;
using Path = System.IO.Path;

namespace Common;

public static class TextHelper {
	public static List<List<Vector2>> SkiaRender(string text, string font, float size) => Render(text, font, size);

	public static List<List<Vector2>> Render(string text, string font, float size) {
		const int width = 600;
		const int height = 400;
		var svgPath = Path.GetTempFileName();
		try {
			using(var surface = new SvgSurface(svgPath, width, height)) {
				using var context = new Context(surface);
				context.SelectFontFace(font, FontSlant.Normal, FontWeight.Normal);
				context.SetFontSize(size);

				var extents = context.TextExtents(text);
				context.MoveTo((width - extents.Width) / 2 - extents.XBearing,
				               (height - extents.Height) / 2 - extents.YBearing);
				context.ShowText(text);
			}

			return SvgParser.Load(svgPath, ignoreZ: font.Contains("opf", StringComparison.OrdinalIgnoreCase))
				.Select(path => path.Path).ToList();
		} finally {
			File.Delete(svgPath);
		}
	}
}
