using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EpubMaker
{
	public class Page : BindableBase
	{
		#region Page プロパティ

		private string imagePath = string.Empty;
		public string ImagePath { get => imagePath; set => SetProperty(ref imagePath, value); }

		private string fileName = string.Empty;
		public string FileName { get => fileName; set => SetProperty(ref fileName, value); }

		private bool isExcluded = false;
		public bool IsExcluded { get => isExcluded; set => SetProperty(ref isExcluded, value); }

		public ImageSource Thumbnail { get; }

		#endregion

		#region Page メソッド

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="fileName"></param>
		public Page(string fileName)
		{
			ImagePath = fileName;
			FileName = Path.GetFileName(fileName);
			Thumbnail = CreateImage(fileName);
		}

		/// <summary>
		/// 画像を生成
		/// </summary>
		/// <param name="fileName"></param>
		private static BitmapImage CreateImage(string fileName)
		{
			BitmapImage bitmapImage = new ();
			bitmapImage.BeginInit();
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage.DecodePixelWidth = 120; // Set the desired width for the thumbnail
			bitmapImage.UriSource = new Uri(fileName);
			bitmapImage.EndInit();
			bitmapImage.Freeze(); // Freeze the image to make it cross-thread accessible
			return bitmapImage;
		}

		#endregion
	}
}