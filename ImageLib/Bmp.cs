using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageLib;

public static class Bmp {
	public static Image Load(string name, byte[] data) {
		using var stream = new MemoryStream(data, writable: false);
		using var bitmap = new BmpDecoder().Decode<Rgb24>(Configuration.Default, stream, default);
		var pixels = new byte[checked(bitmap.Width * bitmap.Height * Image.PixelSize(ColorMode.Rgb))];
		bitmap.CopyPixelDataTo(pixels);
		return new Image(ColorMode.Rgb, (bitmap.Width, bitmap.Height), pixels, name);
	}
}
