using Microsoft.Win32;

public interface IFolderBrowserService
{
	string BrowseFolder();
}

public class FolderBrowserService : IFolderBrowserService
{
	public string BrowseFolder()
	{
		OpenFolderDialog dialog = new OpenFolderDialog();
		return dialog.ShowDialog() == true ? dialog.FolderName : string.Empty;
	}
}