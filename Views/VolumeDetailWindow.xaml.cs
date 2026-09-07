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

			DataContextChanged += (s, e) =>
			{
				Visibility visibility = Visibility.Collapsed;
				if (DataContext is Volume volume)
				{
					Title = $"巻の詳細 - {volume.Name}";

					visibility = Visibility.Visible;
				}

				NumberLabel.Visibility = visibility;
				NumberTextBox.Visibility = visibility;
				PublishedDateLabel.Visibility = visibility;
				PublishedDatePicker.Visibility = visibility;
				DescriptionLabel.Visibility = visibility;
				DescriptionTextBox.Visibility = visibility;
			};
		}

		private void OnApply(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		#endregion
	}
}