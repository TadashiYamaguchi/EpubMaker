using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EpubMaker
{
	public class Page : BindableBase
	{
		#region Page プロパティ

		private string fileName = string.Empty;
		public string FileName { get => fileName; set => SetProperty(ref fileName, value); }

		public ImageSource Image { get; }

		private bool isExcluded = true;
		public bool IsExcluded { get => isExcluded; set => SetProperty(ref isExcluded, value); }

		#endregion

		#region Page メソッド

		public Page(string fileName)
		{
			FileName = Path.GetFileName(fileName);
			Image = CreateImage(fileName);
		}

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