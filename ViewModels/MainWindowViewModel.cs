using System.Diagnostics;
using System.IO;
using System.Windows;

namespace EpubMaker
{
	public class MainWindowViewModel : BindableBase
	{
		#region MainWindowViewModel プロパティ

		private readonly IFolderBrowserService folderBrowserService;
		private readonly IMessageBoxService messageBoxService;

		public DelegateCommand WindowClosedCommand { get; }
		public DelegateCommand BrowseDirectoryCommand { get; }
		public DelegateCommand StartConversionCommand { get; }

		private readonly string tempRootDirectory = Path.Combine( Path.GetTempPath(), "EpubMaker" );

		public ObservableCollectionEx<Volume> Volumes { get; } = [];

		private Volume? selectedVolume = null;
		public Volume? SelectedVolume { get => selectedVolume; set => SetProperty(ref selectedVolume, value); }

		private bool isConverting = false;

		private string outputDirectory = Settings.Default.OutputDirectory;
		public string OutputDirectory
		{
			get => outputDirectory;
			set
			{
				if ( SetProperty(ref outputDirectory, value) )
				{
					Settings.Default.OutputDirectory = value;
					Settings.Default.Save();
				}
			}
		}


		public string[] DropFiles
		{
			set
			{
				if (value != null)
				{
					Action<string, string> addVolume = (sourceFile, extractDirectory) =>
					{
						// 巻リストを生成
						Volume volume = new (sourceFile);
						Volumes.Add(volume);
						// 巻の展開処理を非同期で開始
						_ = volume.LoadAsync(extractDirectory);
					};

					foreach (string fileName in value)
					{
						// ディレクトリの場合
						if ( Directory.Exists(fileName) )
						{
							List<string> volumeDirectories = FindDirectories( fileName, dir => Directory.EnumerateFiles(dir).Any( f => Volume.ImageExtensions.Contains( Path.GetExtension(f).ToLowerInvariant() ) ) );
							foreach (string volumeDirectory in volumeDirectories)
							{
								addVolume(volumeDirectory, volumeDirectory);
							}
						}
						// ファイル場合
						else
						{
							// 一意な一次的フォルダを作成
							string extractDirectory = Path.Combine( tempRootDirectory, Guid.NewGuid().ToString("N") );
							Directory.CreateDirectory(extractDirectory);

							addVolume(fileName, extractDirectory);
						}
					}

					DelegateCommand.ReiseCanExecuteChange();
				}
			}
		}

		#endregion

		#region MainWindowViewModel メソッド

		// <summary>
		// コンストラクタ
		// </summary>
		public MainWindowViewModel(IFolderBrowserService folderBrowserService, IMessageBoxService messageBoxService)
		{
			this.folderBrowserService = folderBrowserService;
			this.messageBoxService = messageBoxService;

			WindowClosedCommand = new (OnWindowClosed);
			BrowseDirectoryCommand = new (OnBrowseDirectory);
			StartConversionCommand = new ( OnStartConversion, () => Volumes.Count > 0 && !isConverting && !string.IsNullOrWhiteSpace(outputDirectory) );
		}

		/// <summary>
		/// アプリケーション終了イベント
		/// </summary>
		private void OnWindowClosed()
		{
			try
			{
				// 一時フォルダを削除
				if ( Directory.Exists(tempRootDirectory) )
				{
					Directory.Delete(tempRootDirectory, recursive: true);
				}
			}
			catch (Exception)
			{
			}
		}

		/// <summary>
		/// 参照ボタン押下イベント
		/// </summary>
		private void OnBrowseDirectory()
		{
			OutputDirectory = folderBrowserService.BrowseFolder();

			DelegateCommand.ReiseCanExecuteChange();
		}

		/// <summary>
		/// 変換開始イベント
		/// </summary>
		private async void OnStartConversion()
		{
			isConverting = true;
			DelegateCommand.ReiseCanExecuteChange();

			foreach (Volume volume in Volumes.Where(v => v.IsTarget) )
			{
				await volume.ConvertToEpubAsync(OutputDirectory);
			}

			isConverting = false;
			DelegateCommand.ReiseCanExecuteChange();

			messageBoxService.Show("変換が完了しました。", Application.Current.MainWindow.Title, MessageBoxButton.OK, MessageBoxImage.Information);
		}

		/// <summary>
		/// 条件に一致するディレクトリを再帰的に検索
		/// </summary>
		/// <param name="directory"></param>
		/// <param name="predicate"></param>
		private static List<string> FindDirectories(string directory, Func<string, bool> predicate)
		{
			if ( predicate(directory) )
			{
				return [directory];
			}

			List<string> result = [];

			try
			{
				foreach (string subDirectory in Directory.GetDirectories(directory))
				{
					result.AddRange(FindDirectories(subDirectory, predicate));
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}

			return result;
		}

		#endregion
	}
}