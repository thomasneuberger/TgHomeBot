using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgHomeBot.Notifications.Contract;
using TgHomeBot.Notifications.Telegram.Commands;
using TgHomeBot.Notifications.Telegram.Services;

namespace TgHomeBot.Notifications.Telegram;
internal class TelegramConnector(
    IOptions<TelegramOptions> options,
    IRegisteredChatService registeredChatService,
    IEnumerable<ICommand> commands,
    ILogger<TelegramConnector> logger)
    : INotificationConnector
{
    private readonly IDictionary<string, ICommand> _commands = commands.ToDictionary(c => c.Name, c => c);
    private readonly TelegramBotClient _botClient = new(options.Value.Token);
	private readonly CancellationTokenSource _cancellationTokenSource = new();

    public async Task Connect()
	{
		await registeredChatService.LoadRegisteredChats();

        var botCommands = commands
	        .Where(c => !c.HideFromMenu)
            .Select(c => new BotCommand { Command = c.Name, Description = c.Description })
            .ToList();

        foreach (var chat in registeredChatService.RegisteredChats)
        {
            await _botClient.SetMyCommandsAsync(botCommands, BotCommandScope.Chat(new ChatId(chat.ChatId)), cancellationToken: _cancellationTokenSource.Token);
        }

		_botClient.StartReceiving(ReceiveUpdate, HandleError, cancellationToken: _cancellationTokenSource.Token);

		var bot = await _botClient.GetMeAsync();

		logger.LogInformation("Telegram bot connected: {Bot}", bot);
	}

	public async Task DisconnectAsync()
	{
		await _cancellationTokenSource.CancelAsync();

		registeredChatService.Clear();

		logger.LogInformation("Telegram bot disconnected.");
	}

	private void HandleError(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
	{
		logger.LogError(exception, "Exception from Telegram Bot: {Exception}", exception);
	}

	private void ReceiveUpdate(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
	{
		ReceiveUpdateAsync(client, update, cancellationToken).GetAwaiter().GetResult();
	}

	private async Task ReceiveUpdateAsync(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
	{
		logger.LogInformation("Update received from Telegram: {Update}", update.Message?.Text ?? "No message");

		if (update is not { Message: { From.Username: not null, Text: not null } })
		{
			return;
		}

		var commandText = update.Message.Text.Split('_', StringSplitOptions.RemoveEmptyEntries)[0];
        if (_commands.TryGetValue(commandText, out var command))
        {
            if (command.AllowUnregistered || registeredChatService.RegisteredChats.Any(c => c.ChatId == update.Message.Chat.Id))
            {
                await command.ProcessMessage(update.Message, client, cancellationToken);
            }
        }
        else
        {
            logger.LogDebug("Received unknown message: {Message}", update.Message.Text);
        }
    }

	public async Task SendAsync(string message)
	{
		foreach (var registeredChat in registeredChatService.RegisteredChats)
		{
			await _botClient.SendTextMessageAsync(registeredChat.ChatId, message, parseMode: ParseMode.Html);

			logger.LogInformation("Message sent to chat {ChatId} with user {User}: {Message}", registeredChat.ChatId, registeredChat.Username, message);
		}
	}
}
