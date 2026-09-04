using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

using SharpCompress.Archives;
using SharpCompress.Common;

namespace EpubMaker
{
	public class Volume : BindableBase
	{
		#region Volume プロパティ

		public DelegateCommand<OutputImageFormats> OutputImageFormatCommand { get; }
		public DelegateCommand<ReadingDirections> ReadingDirectionCommand { get; }
		public DelegateCommand AutoExcludeBlankPagesCommand { get; }
		public DelegateCommand VolumeDetailCommand { get; }

		// 巻タイトル
		private string name = string.Empty;
		public string Name { get => name; set => SetProperty(ref name, value); }

		// 巻番号
		private int? number;
		public int? Number { get => number; set => SetProperty(ref number, value); }

		// シリーズ名
		private string series = string.Empty;
		public string Series { get => series; set => SetProperty(ref series, value); }

		// 著者名
		private string author = string.Empty;
		public string Author { get => author; set => SetProperty(ref author, value); }

		// 出版社名
		private string publisher = string.Empty;
		public string Publisher { get => publisher; set => SetProperty(ref publisher, value); }

		// 発行日
		private DateTime? publishedDate;
		public DateTime? PublishedDate { get => publishedDate; set => SetProperty(ref publishedDate, value); }

		// あらすじ
		private string description = string.Empty;
		public string Description { get => description; set => SetProperty(ref description, value); }

		private string sourceFileName = string.Empty;
		public ObservableCollectionEx<Page> Pages { get; } = [];

		private int count = 0;
		public int Count { get => count; set => SetProperty(ref count, value); }

		private bool isTarget = true;
		public bool IsTarget { get => isTarget; set => SetProperty(ref isTarget, value); }

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

		public string OutputFileName => $"{name}.epub";

		public enum OutputImageFormats
		{
			Jpg,	//(デフォルト)
			WebP
		}
		private OutputImageFormats outputImageFormat = OutputImageFormats.Jpg;
		public OutputImageFormats OutputImageFormat { get => outputImageFormat; set => SetProperty(ref outputImageFormat, value); }


		public enum ReadingDirections
		{
			RightToLeft,    // 漫画想定(デフォルト 右開き)
			LeftToRight
		}

		private ReadingDirections readingDirection = ReadingDirections.RightToLeft;
		public ReadingDirections ReadingDirection { get => readingDirection; set => SetProperty(ref readingDirection, value); }
		private string ReadingDirectionValue => readingDirection == ReadingDirections.RightToLeft ? "rtl" : "ltr";

		public static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".gif", ".webp" };

		private static readonly UTF8Encoding Utf8NoBom = new (false);

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

			OutputImageFormatCommand = new (format => OutputImageFormat = format);
			ReadingDirectionCommand = new (direction => ReadingDirection = direction);
			AutoExcludeBlankPagesCommand = new ( async () => await AutoExcludeBlankPagesAsync() );
			VolumeDetailCommand = new (OnVolumeDetail, () => status >= VolumeStatus.Ready && status != VolumeStatus.Error);
		}

		/// <summary>
		/// 自動で空白ページを除外
		/// </summary>
		private async Task AutoExcludeBlankPagesAsync()
		{
			List<Page> targets = [.. Pages];

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
		/// 巻の詳細を表示
		/// </summary>
		private void OnVolumeDetail()
		{
			var snapshot = (Series, Number, Author, Publisher, PublishedDate, Description);

			VolumeDetailWindow dialog = new () { DataContext = this };
			bool? result = dialog.ShowDialog();
			if (result != true)
			{
				(Series, Number, Author, Publisher, PublishedDate, Description) = snapshot;
			}
		}

		/// <summary>
		/// 巻リストを生成
		/// </summary>
		/// <param name="extractDirectory"></param>
		public async Task LoadAsync(string extractDirectory)
		{
			SetProperty( ref status, VolumeStatus.Extracting, nameof(Status) );
			DelegateCommand.ReiseCanExecuteChange();

			try
			{
				var result = await Task.Run( () =>
				{
					if ( !Directory.Exists(sourceFileName) )
					{
						// アーカイブを開いて全エントリを展開
						using ( IArchive archive = ArchiveFactory.OpenArchive(sourceFileName, new SharpCompress.Readers.ReaderOptions { ArchiveEncoding = new ArchiveEncoding { Default = Encoding.GetEncoding(932) } } ) )
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

					List<Page> pages = Directory.EnumerateFiles(extractDirectory, "*.*", SearchOption.AllDirectories).Where( f => ImageExtensions.Contains( Path.GetExtension(f).ToLowerInvariant() ) ).Select( f => new Page(f) ).ToList();
					string? contentOpf = Directory.EnumerateFiles(extractDirectory, "content.opf", SearchOption.AllDirectories).FirstOrDefault();
					bool hasMetadata = contentOpf != null;
					var metadata = hasMetadata? ParseMetadata(contentOpf!) : (string.Empty, null, string.Empty, string.Empty, null, string.Empty);

					return (Pages: pages, Metadata: metadata, hasMetadata);
				} );

				Count = result.Pages.Count;
				Pages.Clear();
				Pages.AddRange(result.Pages);

				if (result.hasMetadata)
				{
					Series = result.Metadata.Series;
					Number = result.Metadata.Number;
					Author = result.Metadata.Author;
					Publisher = result.Metadata.Publisher;
					publishedDate = result.Metadata.publishedDate;
					Description = result.Metadata.Description;
				}

				SetProperty( ref status, VolumeStatus.Ready, nameof(Status) );
			}
			catch (Exception ex)
			{
				SetProperty( ref status, VolumeStatus.Error, nameof(Status) );

				Debug.WriteLine(ex);
			}

			DelegateCommand.ReiseCanExecuteChange();
		}

		/// <summary>
		/// Epub形式に変換
		/// </summary>
		/// <param name="outputDirectory"></param>
		public async Task ConvertToEpubAsync(string outputDirectory)
		{
			SetProperty( ref status, VolumeStatus.Converting, nameof(Status) );
			DelegateCommand.ReiseCanExecuteChange();

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

			DelegateCommand.ReiseCanExecuteChange();
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
		/// 出力ファイルの拡張子を取得
		/// </summary>
		/// <param name="fileName"></param>
		private string OutputExtension(string fileName)
		{
			string sourceExtension = Path.GetExtension(fileName).ToLowerInvariant();
			string targetExtension = outputImageFormat == OutputImageFormats.WebP ? ".webp" : ".jpg";

			bool alreadyMatches = sourceExtension == targetExtension || (targetExtension == ".jpg" && sourceExtension == ".jpeg");

			return alreadyMatches ? sourceExtension : targetExtension;
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
				WriteTextEntry( archive, "OEBPS/nav.xhtml", BuildNavXhtml(pages) );

				for (int i = 0; i < pages.Count; i++)
				{
					string pageNumber = (i + 1).ToString("D4");
					string sourceExtension = Path.GetExtension(pages[i].ImagePath).ToLowerInvariant();
					string outputExtension = OutputExtension(pages[i].ImagePath);

					if (sourceExtension == ".jpg" || sourceExtension == ".jpeg")
					{
						// 既にJPEGならそのままコピー(再エンコードによる劣化を避ける)
						archive.CreateEntryFromFile(pages[i].ImagePath, $"OEBPS/images/page{pageNumber}{outputExtension}");
					}
					else
					{
						// JPEG以外(WebP含む)はImageLoaderで読み込んでJPEGに変換
						byte[] convertedBytes = outputImageFormat == OutputImageFormats.WebP? ImageLoader.ConvertToWebp(pages[i].ImagePath) : ImageLoader.ConvertToJpeg(pages[i].ImagePath);
						ZipArchiveEntry imageEntry = archive.CreateEntry($"OEBPS/images/page{pageNumber}{outputExtension}", CompressionLevel.Optimal);
						using ( Stream imageStream = imageEntry.Open() )
						{
							imageStream.Write(convertedBytes, 0, convertedBytes.Length);
						}
					}
					// XHTMLファイルを追加
					WriteTextEntry( archive, $"OEBPS/text/page{pageNumber}.xhtml", BuildPageXhtml(pageNumber, outputExtension) );
				}
			}
		}

		/// <summary>
		/// テキストエントリをZIPアーカイブに書き込む
		/// </summary>
		/// <param name="archive"></param>
		/// <param name="entryName"></param>
		/// <param name="content"></param>
		private static void WriteTextEntry(ZipArchive archive, string entryName, string content)
		{
			ZipArchiveEntry entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
			using ( StreamWriter writer = new ( entry.Open(), Utf8NoBom ) )
			{
				writer.Write(content);
			}
		}

		/// <summary>
		/// container.xmlを生成
		/// </summary>
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

		/// <summary>
		/// content.opfを生成
		/// </summary>
		/// <param name="pages"></param>
		private string BuildContentOpf(List<Page> pages)
		{
			StringBuilder manifest = new ();
			StringBuilder spine = new ();

			for (int i = 0; i < pages.Count; i++)
			{
				string pageNumber = (i + 1).ToString("D4");
				string extension = OutputExtension(pages[i].ImagePath);
				string mediaType = extension switch
				{
					".jpg" or ".jpeg" => "image/jpeg",
					".webp" => "image/webp",
					_ => "application/octet-stream"
				};
				string coverProperty = (i == 0) ? " properties=\"cover-image\"" : "";

				manifest.AppendLine($"""		<item id="img{pageNumber}" href="images/page{pageNumber}{extension}" media-type="{mediaType}"{coverProperty}/>""");
				manifest.AppendLine($"""		<item id="text{pageNumber}" href="text/page{pageNumber}.xhtml" media-type="application/xhtml+xml"/>""");
				spine.AppendLine($"""		<itemref idref="text{pageNumber}"/>""");
			}

			StringBuilder seriesMeta = new ();
			if ( !string.IsNullOrWhiteSpace(series) )
			{
				seriesMeta.AppendLine($"""	<meta property="belongs-to-collection" id="series">{series}</meta>""");
				seriesMeta.AppendLine($"""	<meta refines="#series" property="collection-type">series</meta>""");

				if (number.HasValue)
				{
					seriesMeta.AppendLine($"""	<meta refines="#series" property="group-position">{number.Value}</meta>""");
				}
			}

			string creatorElement = string.IsNullOrWhiteSpace(author) ? "" : $"""	<dc:creator>{author}</dc:creator>""";
			string publisherElement = string.IsNullOrWhiteSpace(publisher) ? "" : $"""	<dc:publisher>{publisher}</dc:publisher>""";
			string publishedDateElement = PublishedDate.HasValue ? $"""	<dc:date>{PublishedDate.Value:yyyy-MM-dd}</dc:date>""" : "";
			string descriptionElement = string.IsNullOrWhiteSpace(description) ? "" : $"""	<dc:description>{description}</dc:description>""";

			return
				$"""
				<?xml version="1.0" encoding="UTF-8"?>
				<package version="3.0" unique-identifier="BookId" xmlns="http://www.idpf.org/2007/opf">
					<metadata xmlns:dc="http://purl.org/dc/elements/1.1/">
						<dc:identifier id="BookId">urn:uuid:{Guid.NewGuid()}</dc:identifier>
						<dc:title>{name}</dc:title>
						{creatorElement}
						{publisherElement}
						{publishedDateElement}
						{descriptionElement}
						<dc:language>ja</dc:language>
						<meta property="dcterms:modified">{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</meta>
						<meta property="rendition:layout">pre-paginated</meta>
						<meta property="rendition:orientation">auto</meta>
						<meta property="rendition:spread">auto</meta>
						{seriesMeta}
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

		/// <summary>
		/// nav.xhtmlを生成
		/// </summary>
		/// <param name="pages"></param>
		private static string BuildNavXhtml(List<Page> pages)
		{
			StringBuilder pageList = new ();
			for (int i = 0; i < pages.Count; i++)
			{
				string pageNumber = (i + 1).ToString("D4");
				pageList.AppendLine($"""			<li><a href="text/page{pageNumber}.xhtml">page{pageNumber}</a></li>""");
			}

			return
				$"""
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
						<nav epub:type="page-list" id="page-list" hidden="">
							<ol>
								{pageList}
							</ol>
						</nav>
					</body>
				</html>
				""";
		}

		/// <summary>
		/// pageXhtmlを生成
		/// </summary>
		/// <param name="pageNumber"></param>
		/// <param name="extension"></param>
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

		private static (string Series, int? Number, string Author, string Publisher, DateTime? publishedDate, string Description) ParseMetadata(string contentOpf)
		{
			XNamespace opf = "http://www.idpf.org/2007/opf";
			XNamespace dc = "http://purl.org/dc/elements/1.1/";

			XDocument doc = XDocument.Load(contentOpf);
			XElement? metadata = doc.Root?.Element(opf + "metadata");

			string series = string.Empty;
			int? number = null;

			XElement? collectionMeta = metadata?.Elements(opf + "meta").FirstOrDefault( m => (string?)m.Attribute("property") == "belongs-to-collection" );
			if (collectionMeta != null)
			{
				series = collectionMeta.Value;
				string? id = (string?)collectionMeta.Attribute("id");

				XElement? positionMeta = metadata?.Elements(opf + "meta").FirstOrDefault( m => (string?)m.Attribute("refines") == $"#{id}" && (string?)m.Attribute("property") == "group-position" );
				if (positionMeta != null && int.TryParse(positionMeta.Value, out int parsedNumber))
				{
					number = parsedNumber;
				}
			}

			string author = metadata?.Element(dc + "creator")?.Value ?? string.Empty;
			string publisher = metadata?.Element(dc + "publisher")?.Value ?? string.Empty;

			DateTime? publishedDate = null;
			XElement? dateElement = metadata?.Element(dc + "date");
			if ( dateElement != null && DateTime.TryParseExact(dateElement.Value, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None,	out DateTime parsedDate) )
			{
				publishedDate = parsedDate;
			}

			string description = metadata?.Element(dc + "description")?.Value ?? string.Empty;

			return (series, number, author, publisher, publishedDate, description);
		}

		#endregion
	}
}