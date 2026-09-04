using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EpubMaker
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		#region MainWindow プロパティ
		#endregion

		#region MainWindow メソッド

		public MainWindow()
		{
			InitializeComponent();
		}

		private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (sender is ListViewItem { DataContext: Volume volume } && volume.VolumeDetailCommand.CanExecute(null) )
			{
				volume.VolumeDetailCommand.Execute(null);
			}
		}

		#endregion
	}
}