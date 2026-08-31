using System.IO;
using System.Text;

using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace EpubMaker
{
	public class Volume : BindableBase
	{
		#region Volume プロパティ

		public enum volumeStatus
		{
			Unprocessed,	// 未処理
			Extracting,		// 解凍中
			Scanning,		// 白紙スキャン中
			Ready,			// チェック待ち
			Checked,		// チェック完了
			Converting,		// EPUB変換中
			Completed,		// 完了
			Error			// エラー
		}

		private string sourceFileName = string.Empty;

		private bool isTarget = true;
		public bool IsTarget { get => isTarget; set => SetProperty(ref isTarget, value); }

		private string name = string.Empty;
		public string Name { get => name; set => SetProperty(ref name, value); }

		private volumeStatus status = volumeStatus.Unprocessed;
		public string Status
		{
			get
			{
				return status switch
				{
					volumeStatus.Unprocessed =>	"未処理",
					volumeStatus.Extracting =>	"解凍中",
					volumeStatus.Scanning =>	"白紙スキャン中",
					volumeStatus.Ready =>		"チェック待ち",
					volumeStatus.Checked =>		"チェック完了",
					volumeStatus.Converting =>	"EPUB変換中",
					volumeStatus.Completed =>	"完了",
					volumeStatus.Error =>		"エラー",
					_ => throw new ArgumentOutOfRangeException()
				};
			}
		}

		private int count = 0;
		public int Count { get => count; set => SetProperty(ref count, value); }

		public string OutputFileName => $"{name}.epub";

		public ObservableCollectionEx<Page> Pages { get; } = [];

		private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".webp" };

		#endregion

		#region Volume メソッド

		static Volume()
		{
			// SharpCompressの文字コードをShift_JISに設定
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		}

		public Volume(string fileName)
		{
			sourceFileName = fileName;
			Name = Path.GetFileNameWithoutExtension(fileName);
			status = volumeStatus.Unprocessed;
		}

		public async Task LoadAsync(string extractDirectory)
		{
			SetProperty( ref status, volumeStatus.Extracting, nameof(Status) );

			try
			{
				List<Page> pages = await Task.Run( () =>
				{
					// アーカイブを開いて全エントリを展開
					using ( IArchive archive = ArchiveFactory.OpenArchive(sourceFileName, new ReaderOptions { ArchiveEncoding = new ArchiveEncoding { Default = Encoding.GetEncoding(932) } } ) )
					{
						foreach (IArchiveEntry entryFile in archive.Entries)
						{
							if ( !entryFile.IsDirectory )
							{
								entryFile.WriteToDirectory(extractDirectory, new ExtractionOptions
								{
									ExtractFullPath = true,
									Overwrite = true
								} );
							}
						}

						return Directory.EnumerateFiles(extractDirectory, "*.*", SearchOption.AllDirectories).Where( f => ImageExtensions.Contains( Path.GetExtension(f).ToLowerInvariant() ) ).Select( f => new Page(f) ).ToList();
					}
				} );

				Count = pages.Count;
				Pages.Clear();
				Pages.AddRange(pages);

				SetProperty( ref status, volumeStatus.Ready, nameof(Status) );
			}
			catch (Exception)
			{
				SetProperty( ref status, volumeStatus.Error, nameof(Status) );
			}
		}

		#endregion
	}
}