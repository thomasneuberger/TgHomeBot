using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgHomeBot.Notifications.Contract;
using TgHomeBot.Notifications.Telegram.Commands;
using TgHomeBot.Notifications.Telegram.Models;
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

	private const int MaxRetries = 5;
	private const int MaxRetryDelaySeconds = 30;

    private string? _botName;
	private volatile bool _isConnected;

    public async Task Connect()
	{
		await registeredChatService.LoadRegisteredChats();

		// Start connection in background to avoid blocking application startup
		_ = Task.Run(async () =>
		{
			try
			{
				await ConnectAsync(_cancellationTokenSource.Token);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Unhandled exception in Telegram connection task: {Exception}", ex.Message);
			}
		});
	}

	private async Task ConnectAsync(CancellationToken cancellationToken)
	{
		var retryCount = 0;

		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				logger.LogInformation("Attempting to connect to Telegram bot (attempt {Attempt}/{MaxRetries})...", retryCount + 1, MaxRetries);

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

				_isConnected = true;

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
				logger.LogError(ex, "Error connecting to Telegram bot (attempt {Attempt}/{MaxRetries}): {Exception}", retryCount, MaxRetries, ex.Message);

				if (retryCount >= MaxRetries)
				{
					logger.LogError("Failed to connect to Telegram bot after {MaxRetries} attempts. Telegram functionality will be unavailable.", MaxRetries);
					return;
				}

				try
				{
					var delaySeconds = Math.Min(MaxRetryDelaySeconds, 1 << retryCount);
					logger.LogInformation("Retrying Telegram connection in {Delay} seconds...", delaySeconds);
					await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
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
		await SendAsync(message, NotificationType.General);
	}

	public async Task SendAsync(string message, NotificationType notificationType)
	{
		if (!_isConnected)
		{
			logger.LogWarning("Cannot send message - Telegram bot is not connected yet.");
			return;
		}

		foreach (var registeredChat in registeredChatService.RegisteredChats)
		{
			// Filter based on notification type and feature flags
			if (!ShouldSendNotification(registeredChat, notificationType))
			{
				logger.LogDebug("Skipping notification of type {NotificationType} for chat {ChatId} due to feature flag", notificationType, registeredChat.ChatId);
				continue;
			}

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

	private static bool ShouldSendNotification(RegisteredChat chat, NotificationType notificationType)
	{
		return notificationType switch
		{
			NotificationType.Eurojackpot => chat.EurojackpotEnabled,
			NotificationType.MonthlyChargingReport => chat.MonthlyChargingReportEnabled,
			NotificationType.DeviceNotification => chat.DeviceNotificationsEnabled,
			NotificationType.General => true,
			_ => true
		};
	}
}
