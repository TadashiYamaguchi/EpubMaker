using System.Windows;

public interface IMessageBoxService
{
	MessageBoxResult Show(string message, string title = "", MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None);
}

public class MessageBoxService : IMessageBoxService
{
	public MessageBoxResult Show(string message, string title, MessageBoxButton button, MessageBoxImage icon)
	{
		return MessageBox.Show(message, title, button, icon);
	}
}