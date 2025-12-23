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

    private string? _botName;

    public async Task Connect()
	{
		await registeredChatService.LoadRegisteredChats();

		// Start connection in background to avoid blocking application startup
		_ = Task.Run(async () => await ConnectAsync(_cancellationTokenSource.Token));
	}

	private async Task ConnectAsync(CancellationToken cancellationToken)
	{
		var retryCount = 0;
		const int maxRetries = 5;

		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				logger.LogInformation("Attempting to connect to Telegram bot (attempt {Attempt}/{MaxRetries})...", retryCount + 1, maxRetries);

				var bot = await _botClient.GetMeAsync(cancellationToken);
				_botName = bot.Username;

				logger.LogInformation("Telegram bot connected: {Bot}", bot);

				var botCommands = commands
					.Where(c => !c.HideFromMenu)
					.Select(c => new BotCommand { Command = c.Name, Description = c.Description })
					.ToList();

				foreach (var chat in registeredChatService.RegisteredChats)
				{
					try
					{
						await _botClient.SetMyCommandsAsync(botCommands, BotCommandScope.Chat(new ChatId(chat.ChatId)), cancellationToken: cancellationToken);
					}
					catch (Exception ex)
					{
						logger.LogWarning(ex, "Failed to set commands for chat {ChatId}", chat.ChatId);
					}
				}

				_botClient.StartReceiving(ReceiveUpdate, HandleError, cancellationToken: cancellationToken);

				// Connection successful, exit retry loop
				return;
			}
			catch (OperationCanceledException)
			{
				logger.LogInformation("Telegram bot connection cancelled during shutdown.");
				return;
			}
			catch (Exception ex)
			{
				retryCount++;
				logger.LogError(ex, "Error connecting to Telegram bot (attempt {Attempt}/{MaxRetries}): {Exception}", retryCount, maxRetries, ex.Message);

				if (retryCount >= maxRetries)
				{
					logger.LogError("Failed to connect to Telegram bot after {MaxRetries} attempts. Telegram functionality will be unavailable.", maxRetries);
					return;
				}

				try
				{
					var delay = TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, retryCount)));
					logger.LogInformation("Retrying Telegram connection in {Delay} seconds...", delay.TotalSeconds);
					await Task.Delay(delay, cancellationToken);
				}
				catch (OperationCanceledException)
				{
					logger.LogInformation("Telegram bot connection retry cancelled during shutdown.");
					return;
				}
			}
		}
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

        if (commandText.Contains("@") && !string.IsNullOrWhiteSpace(_botName))
        {
            var parts = commandText.Split('@');
            if (parts.Contains(_botName))
            {
                commandText = parts[0];
            }
        }

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
		if (string.IsNullOrWhiteSpace(_botName))
		{
			logger.LogWarning("Cannot send message - Telegram bot is not connected yet.");
			return;
		}

		foreach (var registeredChat in registeredChatService.RegisteredChats)
		{
			try
			{
				await _botClient.SendTextMessageAsync(registeredChat.ChatId, message, parseMode: ParseMode.Html);
				logger.LogInformation("Message sent to chat {ChatId} with user {User}: {Message}", registeredChat.ChatId, registeredChat.Username, message);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to send message to chat {ChatId}: {Exception}", registeredChat.ChatId, ex.Message);
			}
		}
	}
}
