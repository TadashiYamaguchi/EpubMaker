using System.Globalization;
using System.Windows.Data;

public class EnumToBooleanConverter : IValueConverter
{
	#region EnumToBooleanConverter メソッド

	/// <summary>
	/// Enum型の値をBooleanに変換
	/// </summary>
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return value?.ToString() == parameter?.ToString();
	}

	/// <summary>
	/// BooleanをEnum型の値に変換
	/// </summary>
	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return (bool)value ? Enum.Parse( targetType, parameter?.ToString()! ) : Binding.DoNothing;
	}

	#endregion
}
