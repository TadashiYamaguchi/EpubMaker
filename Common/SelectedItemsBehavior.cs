using Microsoft.Xaml.Behaviors;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

public class SelectedItemsBehavior : Behavior<Selector>
{
	public static readonly DependencyProperty SelectedItemsProperty =
		DependencyProperty.Register( nameof(SelectedItems), typeof(IList), typeof(SelectedItemsBehavior), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault) );

	public IList SelectedItems
	{
		get => (IList)GetValue(SelectedItemsProperty);
		set => SetValue(SelectedItemsProperty, value);
	}

	protected override void OnAttached()
	{
		base.OnAttached();
		AssociatedObject.SelectionChanged += OnSelectionChanged;
	}

	protected override void OnDetaching()
	{
		base.OnDetaching();
		AssociatedObject.SelectionChanged -= OnSelectionChanged;
	}

	private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (sender != null)
		{
			var selector = AssociatedObject;

			if (e.AddedItems != null && e.AddedItems.Count > 0 && SelectedItems != null)
			{
				foreach (var item in e.AddedItems)
				{
					SelectedItems.Add(item);
				}
			}

			if (e.RemovedItems != null && e.RemovedItems.Count > 0 && SelectedItems != null)
			{
				foreach (var item in e.RemovedItems)
				{
					SelectedItems.Remove(item);
				}
			}
		}
	}
}
