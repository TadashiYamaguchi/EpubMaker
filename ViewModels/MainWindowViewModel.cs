using System.IO;

using System.Diagnostics;

namespace EpubMaker
{
	public class MainWindowViewModel : BindableBase
	{
		#region MainWindowViewModel プロパティ

		public ObservableCollectionEx<Volume> Volumes { get; } = [];

		private Volume? selectedVolume = null;
		public Volume? SelectedVolume { get => selectedVolume; set => SetProperty(ref selectedVolume, value); }

		public string[] DropFiles
		{
			set
			{
				if (value != null)
				{
					foreach (string fileName in value)
					{
						if ( Directory.Exists(fileName) )
						{
							string[] subDirectories = Directory.GetDirectories(fileName);

							// 子ディレクトリがある場合
							if (subDirectories.Length > 0)
							{
								foreach (string subDirectory in subDirectories)
								{
									Debug.WriteLine($"巻フォルダ: {subDirectory}");
								}
							}
							// 子ディレクトリがない場合(自身が巻ディレクトリ)
							else
							{
								Debug.WriteLine($"巻フォルダ: {fileName}");
							}
						}
						// ファイル場合(自身が巻ディレクトリ)
						else
						{
							// 一意な一次的フォルダを作成
							string extractDirectory = Path.Combine( Path.GetTempPath(), "EpubMaker", Guid.NewGuid().ToString("N") );
							Directory.CreateDirectory(extractDirectory);

							// 巻名リストに反映
							Volume volume = new (fileName);
							Volumes.Add(volume);

							// 巻の展開処理を非同期で開始
							_ = volume.LoadAsync(extractDirectory);
						}
					}
				}
			}
		}

		#endregion

		#region MainWindowViewModel メソッド
		#endregion
	}
}