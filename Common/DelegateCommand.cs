using System.Windows.Input;

public class DelegateCommand : ICommand
{
	#region DelegateCommandのプロパティ

	private Action execute { get; }         // 実行するアクション
	private Func<bool> canExecute { get; }  // 実行可能かどうかを判定

	#endregion

	#region DelegateCommand メソッド

	/// <summary>
	/// コンストラクタ
	/// </summary>
	public DelegateCommand(Action execute) : this( execute, () => { return true; } )
	{
	}
	public DelegateCommand(Action execute, Func<bool> canExecute)
	{
		this.execute    = execute;
		this.canExecute = canExecute;
	}

	/// <summary>
	/// CanExecute の状態を変更
	/// </summary>
	public static void ReiseCanExecuteChange()
	{
		CommandManager.InvalidateRequerySuggested();
	}

	#endregion

	#region ICommand メンバ

	/// <summary>
	/// コマンドの実行可能状態 変更イベント
	/// </summary>
	public event EventHandler? CanExecuteChanged
	{
		add
		{
			CommandManager.RequerySuggested += value;
		}
		remove
		{
			CommandManager.RequerySuggested -= value;
		}
	}

	/// <summary>
	/// コマンドの実行可能状態を取得
	/// </summary>
	public bool CanExecute(object? parameter)
	{
		return canExecute == null || canExecute.Invoke();
	}

	/// <summary>
	/// コマンドを実行
	/// </summary>
	public void Execute(object? parameter)
	{
		execute.Invoke();
	}

	#endregion
}

public class DelegateCommand<T> : ICommand
{
	#region DelegateCommand<T> プロパティ

	private Action<T> execute { get; }			// 実行するアクション
	private Func<T, bool> canExecute { get; }   // 実行可能かどうかを判定

	#endregion

	#region DelegateCommand<T> メソッド

	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="execute"></param>
	public DelegateCommand(Action<T> execute) : this( execute, obj => { return true; } )
	{
	}
	public DelegateCommand(Action<T> execute, Func<T, bool> canExecute)
	{
		this.execute    = execute;
		this.canExecute = canExecute;
	}

	/// <summary>
	/// CanExecute の状態を変更
	/// </summary>
	public void ReiseCanExecuteChange()
	{
		CommandManager.InvalidateRequerySuggested();
	}

	#endregion

	#region ICommand メンバ

	/// <summary>
	/// コマンドの実行可能状態 変更イベント
	/// </summary>
	public event EventHandler? CanExecuteChanged
	{
		add
		{
			CommandManager.RequerySuggested += value;
		}
		remove
		{
			CommandManager.RequerySuggested -= value;
		}
	}

	/// <summary>
	/// コマンドの実行可能状態を取得
	/// </summary>
	public bool CanExecute(object? parameter)
	{
		return parameter != null ? canExecute == null || canExecute.Invoke( (T)parameter ) : false;
	}

	/// <summary>
	/// コマンドを実行
	/// </summary>
	public void Execute(object? parameter)
	{
		if (parameter != null)
		{
			execute.Invoke( (T)parameter );
		}
	}

	#endregion
}
