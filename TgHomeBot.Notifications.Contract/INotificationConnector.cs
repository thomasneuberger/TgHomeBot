using TgHomeBot.Notifications.Contract.Requests;

namespace TgHomeBot.Notifications.Contract;

public interface INotificationConnector
{
	Task SendAsync(string message);
	Task SendAsync(string message, NotificationType notificationType);
	Task SendWithFilesAsync(string message, IReadOnlyList<FileAttachment> files, NotificationType notificationType);
	Task Connect();
	Task DisconnectAsync();
}
