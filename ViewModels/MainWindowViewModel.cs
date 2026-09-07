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
		private readonly IProgressService progressService;

		public DelegateCommand WindowClosedCommand { get; }
		public DelegateCommand BrowseDirectoryCommand { get; }
		public DelegateCommand StartConversionCommand { get; }
		public DelegateCommand AllDetailsCommand { get; }
		public DelegateCommand ClearCommand { get; }

		private readonly string tempRootDirectory = Path.Combine( Path.GetTempPath(), "EpubMaker" );

		public ObservableCollectionEx<Volume> Volumes { get; } = [];

		private Volume? selectedVolume = null;
		public Volume? SelectedVolume { get => selectedVolume; set => SetProperty(ref selectedVolume, value); }
		public ObservableCollectionEx<Volume> SelectedVolumes { get; } = [];
		public bool IsSingleSelection => SelectedVolumes.Count <= 1;

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
					List<(Volume volume, string ExtractDirectory)> loadings = [];
					Action<string, string> addVolume = (sourceFile, extractDirectory) =>
					{
						// 巻リストを生成
						Volume volume = new (sourceFile);
						Volumes.Add(volume);
						loadings.Add( (volume, extractDirectory) );
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

					Window owner = Application.Current.MainWindow;
					owner.IsEnabled = false;
					progressService.Start(loadings.Count, "読み込み中");

					async Task LoadAllAsync()
					{
						int completedCount = 0;
						List<Task> loadTasks = loadings.Select(async p =>
						{
							await p.volume.LoadAsync(p.ExtractDirectory);
							completedCount++;
							progressService.Report($"{p.volume.Name}の読み込み中...", completedCount);
						} ).ToList();

						await Task.WhenAll(loadTasks);

						progressService.Complete();
						owner.IsEnabled = true;
					}

					// 巻の展開処理を非同期で開始
					_ = LoadAllAsync();

					DelegateCommand.ReiseCanExecuteChange();
				}
			}
		}

		#endregion

		#region MainWindowViewModel メソッド

		// <summary>
		// コンストラクタ
		// </summary>
		public MainWindowViewModel(IFolderBrowserService folderBrowserService, IMessageBoxService messageBoxService, IProgressService progressService)
		{
			this.folderBrowserService = folderBrowserService;
			this.messageBoxService = messageBoxService;
			this.progressService = progressService;

			WindowClosedCommand = new (OnWindowClosed);
			BrowseDirectoryCommand = new (OnBrowseDirectory);
			StartConversionCommand = new ( OnStartConversion, () => Volumes.Count > 0 && !isConverting && !string.IsNullOrWhiteSpace(outputDirectory) );
			AllDetailsCommand = new (OnAllDetails, () => SelectedVolumes.Count > 0 && !isConverting && SelectedVolumes.All(v => v.IsReady) );
			ClearCommand = new (OnClear, () => Volumes.Count > 0 && !isConverting);
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

			Window owner = Application.Current.MainWindow;
			owner.IsEnabled = false;

			List<Volume> targets = Volumes.Where(v => v.IsTarget).ToList();
			progressService.Start(targets.Count);

			int completedCount = 0;
			foreach (Volume volume in targets)
			{
				progressService.Report($"{volume.Name}のEpubを変換中...", completedCount);
				await volume.ConvertToEpubAsync(OutputDirectory);
				completedCount++;
			}
			
			progressService.Complete();
			owner.IsEnabled = true;

			isConverting = false;
			DelegateCommand.ReiseCanExecuteChange();

			messageBoxService.Show("変換が完了しました。", Application.Current.MainWindow.Title, MessageBoxButton.OK, MessageBoxImage.Information);
		}

		/// <summary>
		/// 全巻詳細イベント
		/// </summary>
		private void OnAllDetails()
		{
			AllDetails dataContext = new ()
			{
				Series = GetCommonValue(SelectedVolumes, v => v.Series),
				Author = GetCommonValue(SelectedVolumes, v => v.Author),
				Publisher = GetCommonValue(SelectedVolumes, v => v.Publisher)
			};

			VolumeDetailWindow dialog = new () { Title = "全巻詳細", DataContext = dataContext };
			if ( dialog.ShowDialog() == true )
			{
				foreach (Volume volume in SelectedVolumes)
				{
					if ( !String.IsNullOrWhiteSpace(dataContext.Series) )
					{
						volume.Series = dataContext.Series;
					}
					if ( !String.IsNullOrWhiteSpace(dataContext.Author) )
					{
						volume.Author = dataContext.Author;
					}
					if ( !String.IsNullOrWhiteSpace(dataContext.Publisher) )
					{
						volume.Publisher = dataContext.Publisher;
					}
				}
			}
		}

		/// <summary>
		/// リストをクリアイベント
		/// </summary>
		private void OnClear()
		{
			Volumes.Clear();
			SelectedVolumes.Clear();
			SelectedVolume = null;

			DelegateCommand.ReiseCanExecuteChange();
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

		/// <summary>
		/// 指定されたボリュームの共通の値を取得
		/// </summary>
		/// <param name="volumes"></param>
		/// <param name="selector"></param>
		private static string GetCommonValue(IEnumerable<Volume> volumes, Func<Volume, string?> selector)
		{
			List<string> distinct = [.. volumes.Select(selector).Distinct()];
			return distinct.Count == 1 ? distinct[0] : string.Empty;
		}

		#endregion
	}
}