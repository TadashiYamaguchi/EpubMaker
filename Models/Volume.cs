using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EpubMaker
{
	public class Volume : BindableBase
	{
		#region Volume プロパティ

		public DelegateCommand AutoExcludeBlankPagesCommand { get; }

		private string sourceFileName = string.Empty;

		private bool isTarget = true;
		public bool IsTarget { get => isTarget; set => SetProperty(ref isTarget, value); }

		private string name = string.Empty;
		public string Name { get => name; set => SetProperty(ref name, value); }

		public enum VolumeStatus
		{
			Unprocessed,    // 未処理
			Extracting,     // 解凍中
			Scanning,       // 白紙スキャン中
			Ready,          // チェック待ち
			Checked,        // チェック完了
			Converting,     // EPUB変換中
			Completed,      // 完了
			Error           // エラー
		}

		private VolumeStatus status = VolumeStatus.Unprocessed;
		public string Status
		{
			get
			{
				return status switch
				{
					VolumeStatus.Unprocessed =>	"未処理",
					VolumeStatus.Extracting =>	"解凍中",
					VolumeStatus.Scanning =>	"白紙スキャン中",
					VolumeStatus.Ready =>		"チェック待ち",
					VolumeStatus.Checked =>		"チェック完了",
					VolumeStatus.Converting =>	"EPUB変換中",
					VolumeStatus.Completed =>	"完了",
					VolumeStatus.Error =>		"エラー",
					_ => throw new ArgumentOutOfRangeException()
				};
			}
		}

		private int count = 0;
		public int Count { get => count; set => SetProperty(ref count, value); }

		public string OutputFileName => $"{name}.epub";

		public ObservableCollectionEx<Page> Pages { get; } = [];

		public enum ReadingDirections
		{
			RightToLeft,    // 漫画想定(デフォルト 右開き)
			LeftToRight
		}

		private ReadingDirections readingDirection = ReadingDirections.RightToLeft;
		public ReadingDirections ReadingDirection { get => readingDirection; set => SetProperty(ref readingDirection, value); }
		private string ReadingDirectionValue => readingDirection == ReadingDirections.RightToLeft ? "rtl" : "ltr";

		private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp" };

		private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

		#endregion

		#region Volume メソッド

		static Volume()
		{
			// SharpCompressの文字コードをShift_JISに設定
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		}

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="fileName"></param>
		public Volume(string fileName)
		{
			sourceFileName = fileName;
			Name = Path.GetFileNameWithoutExtension(fileName);
			status = VolumeStatus.Unprocessed;

			AutoExcludeBlankPagesCommand = new (async () => await AutoExcludeBlankPagesAsync() );

		}

		/// <summary>
		/// 巻リストを生成
		/// </summary>
		/// <param name="extractDirectory"></param>
		public async Task LoadAsync(string extractDirectory)
		{
			SetProperty( ref status, VolumeStatus.Extracting, nameof(Status) );

			try
			{
				List<Page> pages = await Task.Run( () =>
				{
					if ( !Directory.Exists(sourceFileName) )
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
						}
					}
					return Directory.EnumerateFiles(extractDirectory, "*.*", SearchOption.AllDirectories).Where( f => ImageExtensions.Contains( Path.GetExtension(f).ToLowerInvariant() ) ).Select( f => new Page(f) ).ToList();
				} );

				Count = pages.Count;
				Pages.Clear();
				Pages.AddRange(pages);

				SetProperty( ref status, VolumeStatus.Ready, nameof(Status) );
			}
			catch (Exception ex)
			{
				SetProperty( ref status, VolumeStatus.Error, nameof(Status) );

				Debug.WriteLine(ex);
			}
		}

		/// <summary>
		/// Epub形式に変換
		/// </summary>
		/// <param name="outputDirectory"></param>
		public async Task ConvertToEpubAsync(string outputDirectory)
		{
			SetProperty( ref status, VolumeStatus.Converting, nameof(Status) );

			try
			{
				List<Page> targetPages = Pages.Where( p => !p.IsExcluded ).ToList();
				string epubFileName = Path.Combine(outputDirectory, OutputFileName);

				await Task.Run( () => BuildEpub(epubFileName, targetPages) );

				SetProperty( ref status, VolumeStatus.Completed, nameof(Status) );
			}
			catch (Exception ex)
			{
				SetProperty( ref status, VolumeStatus.Error, nameof(Status) );
				Debug.WriteLine(ex);
			}
		}

		/// <summary>
		/// 自動で空白ページを除外
		/// </summary>
		private async Task AutoExcludeBlankPagesAsync()
		{
			List<Page> targets = Pages.ToList();

			bool[] isBlanks = await Task.Run( () =>
			{
				bool[] results = new bool[targets.Count];
				for (int i = 0; i < targets.Count; i++)
				{
					results[i] = IsBlankPage(targets[i].Thumbnail);
				}
				return results;
			} );

			// 判定結果を反映
			for (int i = 0; i < targets.Count; i++)
			{
				targets[i].IsExcluded = isBlanks[i];
			}
		}

		/// <summary>
		/// 画像が空白ページかどうかを判定
		/// </summary>
		/// <param name="imageSource"></param>
		/// <param name="whiteThreshold"></param>
		/// <param name="blankRatioThreshold"></param>
		private static bool IsBlankPage(ImageSource imageSource, byte whiteThreshold = 240, double blankRatioThreshold = 0.98)
		{
			bool result = false;

			if (imageSource is BitmapSource bitmapSource)
			{
				FormatConvertedBitmap grayBitmap = new (bitmapSource, PixelFormats.Gray8, null, 0);
				int width = grayBitmap.PixelWidth;
				int height = grayBitmap.PixelHeight;
				byte[] pixels = new byte[width * height];
				grayBitmap.CopyPixels(pixels, width, 0);

				int whiteCount = pixels.Count( p => p >= whiteThreshold );
				double whiteRatio = (double)whiteCount / pixels.Length;
				result = whiteRatio >= blankRatioThreshold;
			}

			return result;
		}

		/// <summary>
		/// Epubを生成
		/// </summary>
		/// <param name="epubFileName"></param>
		/// <param name="pages"></param>
		private void BuildEpub(string epubFileName, List<Page> pages)
		{
			using (FileStream fileStream = new (epubFileName, FileMode.Create) )
			using ( ZipArchive archive = new (fileStream, ZipArchiveMode.Create) )
			{
				// mimetypeは無圧縮・必ず先頭エントリ(EPUB仕様の必須ルール)
				ZipArchiveEntry mimetypeEntry = archive.CreateEntry("mimetype", CompressionLevel.NoCompression);
				using ( StreamWriter writer = new ( mimetypeEntry.Open(), Utf8NoBom ) )
				{
					writer.Write("application/epub+zip");
				}

				WriteTextEntry( archive, "META-INF/container.xml", BuildContainerXml() );
				WriteTextEntry( archive, "OEBPS/content.opf", BuildContentOpf(pages) );
				WriteTextEntry( archive, "OEBPS/nav.xhtml", BuildNavXhtml() );

				for (int i = 0; i < pages.Count; i++)
				{
					string pageNumber = (i + 1).ToString("D4");
					string extension = Path.GetExtension(pages[i].ImagePath);

					// 画像ファイルを追加
					archive.CreateEntryFromFile(pages[i].ImagePath, $"OEBPS/images/page{pageNumber}{extension}");
					// XHTMLファイルを追加
					WriteTextEntry( archive, $"OEBPS/text/page{pageNumber}.xhtml", BuildPageXhtml(pageNumber, extension) );
				}
			}
		}

		private void WriteTextEntry(ZipArchive archive, string entryName, string content)
		{
			ZipArchiveEntry entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
			using ( StreamWriter writer = new ( entry.Open(), Utf8NoBom ) )
			{
				writer.Write(content);
			}
		}

		private static string BuildContainerXml()
		{
			return
				"""
				<?xml version="1.0" encoding="UTF-8"?>
				<container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container">
				  <rootfiles>
					<rootfile full-path="OEBPS/content.opf" media-type="application/oebps-package+xml"/>
				  </rootfiles>
				</container>
				""";
		}

		private string BuildContentOpf(List<Page> pages)
		{
			StringBuilder manifest = new ();
			StringBuilder spine = new ();

			for (int i = 0; i < pages.Count; i++)
			{
				string pageNumber = (i + 1).ToString("D4");
				string extension = Path.GetExtension(pages[i].ImagePath).ToLowerInvariant();
				string mediaType = extension switch
				{
					".jpg" or ".jpeg" => "image/jpeg",
					".png" => "image/png",
					".gif" => "image/gif",
					".bmp" => "image/bmp",
					".webp" => "image/webp",
					_ => "application/octet-stream"
				};
				string coverProperty = (i == 0) ? " properties=\"cover-image\"" : "";

				manifest.AppendLine($"""		<item id="img{pageNumber}" href="images/page{pageNumber}{extension}" media-type="{mediaType}"{coverProperty}/>""");
				manifest.AppendLine($"""		<item id="text{pageNumber}" href="text/page{pageNumber}.xhtml" media-type="application/xhtml+xml"/>""");
				spine.AppendLine($"""		<itemref idref="text{pageNumber}"/>""");
			}

			return
				$"""
				<?xml version="1.0" encoding="UTF-8"?>
				<package version="3.0" unique-identifier="BookId" xmlns="http://www.idpf.org/2007/opf">
					<metadata xmlns:dc="http://purl.org/dc/elements/1.1/">
						<dc:identifier id="BookId">urn:uuid:{Guid.NewGuid()}</dc:identifier>
						<dc:title>{name}</dc:title>
						<dc:language>ja</dc:language>
						<meta property="rendition:layout">pre-paginated</meta>
						<meta property="rendition:orientation">auto</meta>
						<meta property="rendition:spread">auto</meta>
					</metadata>
					<manifest>
						<item id="nav" href="nav.xhtml" media-type="application/xhtml+xml" properties="nav"/>
						{manifest}
					</manifest>
					<spine page-progression-direction="{ReadingDirectionValue}">
						{spine}
					</spine>
				</package>
				""";
		}

		private static string BuildNavXhtml()
		{
			return
				"""
				<?xml version="1.0" encoding="UTF-8"?>
				<!DOCTYPE html>
				<html xmlns="http://www.w3.org/1999/xhtml" xmlns:epub="http://www.idpf.org/2007/ops">
					<head><title>Navigation</title></head>
					<body>
					  <nav epub:type="toc" id="toc">
						  <ol>
							  <li><a href="text/page0001.xhtml">先頭</a></li>
						  </ol>
					  </nav>
					</body>
				</html>
				""";
		}

		private static string BuildPageXhtml(string pageNumber, string extension)
		{
			return
				$$"""
				<?xml version="1.0" encoding="UTF-8"?>
				<!DOCTYPE html>
				<html xmlns="http://www.w3.org/1999/xhtml">
				  <head>
					<meta charset="UTF-8"/>
					<title>page{{pageNumber}}</title>
					<style>html,body{margin:0;padding:0;} img{width:100%;height:100%;}</style>
				  </head>
				  <body>
					<img src="../images/page{{pageNumber}}{{extension}}" alt=""/>
				  </body>
				</html>
				""";
		}

		#endregion
	}
}