using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using SkiaSharp;

namespace EpubMaker
{
	internal static class ImageLoader
	{
		public static BitmapSource LoadImage(string fileName, int decodePixelWidth = 0)
		{
			string extension = Path.GetExtension(fileName).ToLowerInvariant();
			if (extension == ".webp")
			{
				return LoadWithSkia(fileName, decodePixelWidth);
			}
			else
			{
				BitmapImage result = new();
				result.BeginInit();
				result.CacheOption = BitmapCacheOption.OnLoad;
				if (decodePixelWidth > 0)
				{
					result.DecodePixelWidth = decodePixelWidth;
				}
				result.UriSource = new(fileName);
				result.EndInit();
				result.Freeze(); // Freeze the image to make it cross-thread accessible
				return result;
			}
		}

		public static BitmapSource LoadWithSkia(string fileName, int decodePixelWidth = 0)
		{
			using SKBitmap image = SKBitmap.Decode(fileName);

			int width = decodePixelWidth > 0 ? decodePixelWidth : image.Width;
			int height = decodePixelWidth > 0 ?	(int)Math.Round(image.Height * (double)width / image.Width) : image.Height;

			// WPFのPbgra32と揃えるため、色情報をBgra8888(事前乗算アルファ)に統一
			SKImageInfo imageInfo = new (width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

			// リサイズと色形式変換を同時に行う(フル解像度バッファを経由しない)
			using SKBitmap resized = image.Resize(imageInfo, new (SKFilterMode.Linear, SKMipmapMode.Linear) );
			int stride = resized.RowBytes;
			byte[] pixels = resized.Bytes;

			BitmapSource result = BitmapSource.Create(width, height, 96, 96, PixelFormats.Pbgra32, null, pixels, stride);
			result.Freeze(); // Freeze the image to make it cross-thread accessible
			return result;
		}

		/// <summary>
		/// JPEG形式に変換
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="quality"></param>
		public static byte[] ConvertToJpeg(string fileName, int quality = 90)
		{
			JpegBitmapEncoder encoder = new () { QualityLevel = quality };
			encoder.Frames.Add( BitmapFrame.Create( LoadImage(fileName) ) );

			using MemoryStream memoryStream = new ();
			encoder.Save(memoryStream);
			return memoryStream.ToArray();
		}

		/// <summary>
		/// Webp形式に変換
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="quality"></param>
		/// <returns></returns>
		public static byte[] ConvertToWebp(string fileName, int quality = 90)
		{
			using SKBitmap bitmap = SKBitmap.Decode(fileName);
			using SKImage image = SKImage.FromBitmap(bitmap);
			using SKData data = image.Encode(SKEncodedImageFormat.Webp, quality);
			return data.ToArray();
		}
	}
}