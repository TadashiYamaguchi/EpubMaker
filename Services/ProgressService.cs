using System.Windows;

public interface IProgressService
{
	void Start(int total, string title = "処理中");
	void Report(string message, int current);
	void Complete();
}

public class ProgressService : IProgressService
{
	#region ProgressService プロパティ

	private ProgressWindow? window = null;
	private ProgressWindowViewModel? viewModel = null;
	private Window? owner = null;

	#endregion

	#region ProgressService メソッド

	public void Start(int total, string title = "処理中")
	{
		owner = Application.Current.MainWindow;

		viewModel = new ProgressWindowViewModel { Total = total };
		window = new ProgressWindow { DataContext = viewModel, Owner = owner, Title = title };

		if (owner != null)
		{
			owner.IsEnabled = false;
		}

		window.Show();
	}

	public void Report(string message, int current)
	{
		if (viewModel != null)
		{
			viewModel.Message = message;
			viewModel.Current = current;
		}
	}

	public void Complete()
	{
		window?.Close();

		if (owner != null)
		{
			owner.IsEnabled = true;
		}

		window = null;
		viewModel = null;
		owner = null;
	}

	#endregion
}