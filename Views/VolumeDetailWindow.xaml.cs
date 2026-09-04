using System.Windows;

namespace EpubMaker
{
	/// <summary>
	/// Interaction logic for VolumeDetailWindow.xaml
	/// </summary>
	public partial class VolumeDetailWindow : Window
	{
		#region VolumeDetailWindow プロパティ
		#endregion

		#region VolumeDetailWindow メソッド

		public VolumeDetailWindow()
		{
			InitializeComponent();
		}

		private void OnApply(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		#endregion
	}
}