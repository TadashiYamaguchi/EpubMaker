using System.Windows;

namespace EpubMaker
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		#region Appのメソッド

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			var view = new MainWindow()
			{
				DataContext = new MainWindowViewModel( new FolderBrowserService(), new MessageBoxService() )
			};
			view.Show();
		}

		#endregion
	}
}
