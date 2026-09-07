using Microsoft.Win32;

public interface IFolderBrowserService
{
	string BrowseFolder();
}

public class FolderBrowserService : IFolderBrowserService
{
	#region FolderBrowserService メソッド

	public string BrowseFolder()
	{
		OpenFolderDialog dialog = new ();
		return dialog.ShowDialog() == true ? dialog.FolderName : string.Empty;
	}

	#endregion
}