public class ProgressWindowViewModel : BindableBase
{
	#region ProgressWindowViewModel プロパティ

	public int Total { get; init; }

	public int current = 0;
	public int Current { get => current; set => SetProperty(ref current, value); }

	private string message = string.Empty;
	public string Message { get => message; set => SetProperty(ref message, value); }

	#endregion

	#region ProgressWindowViewModel メソッド
	#endregion
}
