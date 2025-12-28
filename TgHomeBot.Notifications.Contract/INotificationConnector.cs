namespace TgHomeBot.Notifications.Contract;

public interface INotificationConnector
{
	Task SendAsync(string message);
	Task SendAsync(string message, NotificationType notificationType);
	Task Connect();
	Task DisconnectAsync();
}
