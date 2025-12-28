namespace TgHomeBot.Notifications.Telegram.Models;
public class RegisteredChat
{
	public required long Id { get; set; }

	public required string Username { get; set; }

	public required long ChatId { get; set; }

	public bool EurojackpotEnabled { get; set; } = true;

	public bool MonthlyChargingReportEnabled { get; set; } = true;

	public bool DeviceNotificationsEnabled { get; set; } = true;
}
