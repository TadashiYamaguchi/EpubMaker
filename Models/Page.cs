using System.Diagnostics;
using System.IO;
using System.Windows.Media;

namespace EpubMaker
{
	public class Page : BindableBase
	{
		#region Page プロパティ

		public DelegateCommand OpenImageCommand { get; }

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
			OpenImageCommand = new (OnOpenImage);

			ImagePath = fileName;
			FileName = Path.GetFileName(fileName);
			Thumbnail = ImageLoader.LoadImage(fileName, decodePixelWidth: 120);
		}

		/// <summary>
		/// 画像をビューアで表示
		/// </summary>
		private void OnOpenImage()
		{
			try
			{
				Process.Start(new ProcessStartInfo(imagePath) { UseShellExecute = true });
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}
		}

		#endregion
	}
}