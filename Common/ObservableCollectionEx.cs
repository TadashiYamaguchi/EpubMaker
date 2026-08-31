using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

public class ObservableCollectionEx<T> : ObservableCollection<T>
{
	#region ObservableCollectionEx<T> メソッド

	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="items"></param>
	/// <exception cref="ArgumentNullException"></exception>
	public void AddRange(IEnumerator items)
	{
		if (items == null)
		{
			throw new ArgumentNullException( nameof(items) );
		}
		else
		{
			while ( items.MoveNext() )
			{
				Items.Add( (T)items.Current );
			}
		}
		OnCollectionChanged( new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset) );
	}

	/// <summary>
	/// コレクションにアイテムを追加
	/// </summary>
	/// <param name="items"></param>
	/// <exception cref="ArgumentNullException"></exception>
	public void AddRange(IEnumerable<T> items)
	{
		if (items == null)
		{
			throw new ArgumentNullException( nameof(items) );
		}
		else
		{
			foreach (T item in items)
			{
				Items.Add(item);
			}
		}
		OnCollectionChanged( new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset) );
	}

	/// <summary>
	/// コレクションを更新
	/// </summary>
	public void Update()
	{
		OnCollectionChanged( new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset) );
	}

	#endregion
}
