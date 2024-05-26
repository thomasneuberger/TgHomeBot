namespace TgHomeBot.Notifications.Contract;

public interface INotificationConnector
{
	Task SendAsync(string message);
	Task Connect();
	Task DisconnectAsync();
}
