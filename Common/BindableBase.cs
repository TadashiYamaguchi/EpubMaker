using System.ComponentModel;

public class BindableBase : INotifyPropertyChanged
{
	#region BindableBase メソッド

	/// <summary>
	/// プロパティを設定
	/// </summary>
	protected bool SetProperty<T>(ref T field, T value, string propertyName = "")
	{
		// 値が変わらない場合は何もしない
		if ( EqualityComparer<T>.Default.Equals(field, value) )
		{
			return false;
		}

		// 値を設定し、プロパティ変更イベントを発火
		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}

	#endregion

	#region INotifyPropertyChanged メンバ

	/// <summary>
	/// プロパティ変更イベント
	/// </summary>
	public event PropertyChangedEventHandler? PropertyChanged;

	/// <summary>
	/// プロパティが変更されたことを通知
	/// </summary>
	protected void OnPropertyChanged(string propertyName = "")
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	#endregion
}
