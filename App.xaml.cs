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

			MainWindow view = new ()
			{
				DataContext = new MainWindowViewModel( new FolderBrowserService(), new MessageBoxService(), new ProgressService() )
			};
			view.Show();
		}

		#endregion
	}
}
