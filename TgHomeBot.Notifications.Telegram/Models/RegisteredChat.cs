namespace TgHomeBot.Notifications.Telegram.Models;
internal class RegisteredChat
{
	public required long Id { get; set; }

	public required string Username { get; set; }

	public required long ChatId { get; set; }
}
