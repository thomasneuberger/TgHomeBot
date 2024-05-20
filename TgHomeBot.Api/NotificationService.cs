using TgHomeBot.Notifications.Contract;

namespace TgHomeBot.Api;

public class NotificationService(INotificationConnector notificationConnector) : IHostedService
{
	public Task StartAsync(CancellationToken cancellationToken)
	{
		return notificationConnector.Connect();
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		return notificationConnector.DisconnectAsync();
	}
}
