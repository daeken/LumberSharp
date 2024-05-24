using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Cairo;
using Path = System.IO.Path;

namespace Common; 

public static class TextHelper {
	public static List<List<Vector2>> Render(string text, string font, float size) {
		var tfn = Path.GetTempFileName();
		using(var surface = new SvgSurface(tfn, 600, 400)) {
			using var cr = new Context(surface);
			cr.SelectFontFace(font, FontSlant.Normal, FontWeight.Normal);
			cr.SetFontSize(size);
			
			var te = cr.TextExtents(text);
			cr.MoveTo((600 - te.Width) / 2 - te.XBearing, (400 - te.Height) / 2 - te.YBearing);
			cr.ShowText(text);
		}
		Console.WriteLine(tfn);
		var paths = SvgParser.Load(tfn, ignoreZ: font.ToLower().Contains("opf")).Select(x => x.Path).ToList();
		//File.Delete(tfn);
		//paths = paths.RemoveOverlaps();
		return paths;
	}
}