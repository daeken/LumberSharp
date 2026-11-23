using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.IO.Hashing;

namespace ImageLib;

public static class Png {
	static ReadOnlySpan<byte> Signature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

	public static void Encode(Image image, Stream stream) {
		stream.Write(Signature);

		Span<byte> header = stackalloc byte[13];
		BinaryPrimitives.WriteInt32BigEndian(header, image.Size.Width);
		BinaryPrimitives.WriteInt32BigEndian(header[4..], image.Size.Height);
		header[8] = 8;
		header[9] = image.ColorMode switch {
			ColorMode.Greyscale => 0,
			ColorMode.Rgb => 2,
			ColorMode.Rgba => 6,
			_ => throw new NotImplementedException()
		};
		header[10..].Clear(); // Compression method, filter method, and interlace method.
		WriteChunk(stream, "IHDR"u8, header);

		using var compressed = new MemoryStream();
		var stride = image.Size.Width * Image.PixelSize(image.ColorMode);
		using(var compressor = new ZLibStream(compressed, CompressionLevel.Fastest, leaveOpen: true)) {
			for(var y = 0; y < image.Size.Height; ++y) {
				compressor.WriteByte(0); // No filter.
				compressor.Write(image.Data.AsSpan(y * stride, stride));
			}
		}

		WriteChunk(stream, "IDAT"u8, compressed.GetBuffer().AsSpan(0, (int) compressed.Length));
		WriteChunk(stream, "IEND"u8, []);
		stream.Flush();
	}

	static void WriteChunk(Stream stream, ReadOnlySpan<byte> type, ReadOnlySpan<byte> data) {
		Span<byte> number = stackalloc byte[4];
		BinaryPrimitives.WriteInt32BigEndian(number, data.Length);
		stream.Write(number);
		stream.Write(type);
		stream.Write(data);

		var crc = new Crc32();
		crc.Append(type);
		crc.Append(data);
		BinaryPrimitives.WriteUInt32BigEndian(number, crc.GetCurrentHashAsUInt32());
		stream.Write(number);
	}

	public static Image Decode(string fn) {
		using var stream = File.OpenRead(fn);
		return Decode(fn, stream);
	}

	public static Image Decode(string name, Stream stream) {
		Span<byte> signature = stackalloc byte[8];
		stream.ReadExactly(signature);
		if(!signature.SequenceEqual(Signature))
			throw new InvalidDataException("Invalid PNG signature.");

		(int Width, int Height) size = default;
		var colorMode = ColorMode.Greyscale;
		using var compressed = new MemoryStream();
		Span<byte> chunkHeader = stackalloc byte[8];
		Span<byte> header = stackalloc byte[13];
		Span<byte> buffer = stackalloc byte[4096];
		Span<byte> crc = stackalloc byte[4];

		while(true) {
			stream.ReadExactly(chunkHeader);
			var length = BinaryPrimitives.ReadInt32BigEndian(chunkHeader);
			var type = chunkHeader[4..];
			if(length < 0)
				throw new InvalidDataException("Invalid PNG chunk length.");

			if(type.SequenceEqual("IHDR"u8)) {
				if(length != header.Length)
					throw new InvalidDataException("Invalid PNG header length.");
				stream.ReadExactly(header);
				size = (BinaryPrimitives.ReadInt32BigEndian(header), BinaryPrimitives.ReadInt32BigEndian(header[4..]));
				if(size.Width <= 0 || size.Height <= 0)
					throw new InvalidDataException("Invalid PNG dimensions.");
				if(header[8] != 8 || header[10] != 0 || header[11] != 0 || header[12] != 0)
					throw new NotSupportedException("Only non-interlaced 8-bit PNG images are supported.");

				colorMode = header[9] switch {
					0 => ColorMode.Greyscale,
					2 => ColorMode.Rgb,
					6 => ColorMode.Rgba,
					_ => throw new NotImplementedException()
				};
			} else {
				var isImageData = type.SequenceEqual("IDAT"u8);
				while(length > 0) {
					var block = buffer[..Math.Min(length, buffer.Length)];
					stream.ReadExactly(block);
					if(isImageData)
						compressed.Write(block);
					length -= block.Length;
				}
			}

			stream.ReadExactly(crc);
			if(type.SequenceEqual("IEND"u8)) break;
		}

		if(size == default)
			throw new InvalidDataException("Missing PNG header.");

		compressed.Position = 0;
		using var decompressor = new ZLibStream(compressed, CompressionMode.Decompress);
		var pixelSize = Image.PixelSize(colorMode);
		var stride = checked(size.Width * pixelSize);
		var data = new byte[checked(stride * size.Height)];
		for(var y = 0; y < size.Height; ++y) {
			var filter = decompressor.ReadByte();
			if(filter < 0) throw new EndOfStreamException();

			var row = data.AsSpan(y * stride, stride);
			decompressor.ReadExactly(row);
			ReadOnlySpan<byte> previousRow = y == 0 ? [] : data.AsSpan((y - 1) * stride, stride);
			Unfilter(row, previousRow, pixelSize, filter);
		}

		return new Image(colorMode, size, data, name);
	}

	static void Unfilter(Span<byte> row, ReadOnlySpan<byte> previousRow, int pixelSize, int filter) {
		switch(filter) {
			case 0:
				break;
			case 1:
				for(var x = pixelSize; x < row.Length; ++x)
					row[x] = unchecked((byte) (row[x] + row[x - pixelSize]));
				break;
			case 2:
				for(var x = 0; x < previousRow.Length; ++x)
					row[x] = unchecked((byte) (row[x] + previousRow[x]));
				break;
			case 4:
				for(var x = 0; x < row.Length; ++x) {
					var left = x < pixelSize ? 0 : row[x - pixelSize];
					var above = previousRow.IsEmpty ? 0 : previousRow[x];
					var upperLeft = previousRow.IsEmpty || x < pixelSize ? 0 : previousRow[x - pixelSize];
					row[x] = unchecked((byte) (row[x] + Paeth(left, above, upperLeft)));
				}
				break;
			default:
				throw new NotImplementedException($"Unsupported filter mode {filter}");
		}
	}

	static int Paeth(int left, int above, int upperLeft) {
		var prediction = left + above - upperLeft;
		var leftError = Math.Abs(prediction - left);
		var aboveError = Math.Abs(prediction - above);
		var upperLeftError = Math.Abs(prediction - upperLeft);
		return leftError <= aboveError && leftError <= upperLeftError
			? left
			: aboveError <= upperLeftError ? above : upperLeft;
	}
}
