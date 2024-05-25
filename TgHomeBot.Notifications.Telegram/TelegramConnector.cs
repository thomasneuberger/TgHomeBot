using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgHomeBot.Common.Contract;
using TgHomeBot.Notifications.Contract;
using TgHomeBot.Notifications.Telegram.Models;
using File = System.IO.File;

namespace TgHomeBot.Notifications.Telegram;
internal class TelegramConnector : INotificationConnector
{
	private readonly IOptions<TelegramOptions> _options;
	private readonly IOptions<FileStorageOptions> _fileStorageOptions;
	private readonly ILogger<TelegramConnector> _logger;

	private readonly TelegramBotClient _botClient;
	private readonly List<RegisteredChat> _registeredChats = new List<RegisteredChat>();
	private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerOptions.Default)
	{
		WriteIndented = true
	};
	private readonly CancellationTokenSource _cancellationTokenSource = new();

	public TelegramConnector(IOptions<TelegramOptions> options, IOptions<FileStorageOptions> fileStorageOptions, ILogger<TelegramConnector> logger)
	{
		_jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
		_options = options;
		_fileStorageOptions = fileStorageOptions;
		_logger = logger;
		_botClient = new TelegramBotClient(_options.Value.Token);
	}

	public async Task Connect()
	{
		await LoadRegisteredChats();

		_botClient.StartReceiving(ReceiveUpdate, HandleError, cancellationToken: _cancellationTokenSource.Token);

		var bot = await _botClient.GetMeAsync();

		_logger.LogInformation("Telegram bot connected: {Bot}", bot);
	}

	public async Task DisconnectAsync()
	{
		await _cancellationTokenSource.CancelAsync();

		_registeredChats.Clear();

		_logger.LogInformation("Telegram bot disconnected.");
	}

	private void HandleError(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
	{
		_logger.LogError(exception, "Exception from Telegram Bot: {Exception}", exception);
	}

	private void ReceiveUpdate(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
	{
		ReceiveUpdateAsync(client, update, cancellationToken).GetAwaiter().GetResult();
	}

	private async Task ReceiveUpdateAsync(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Update received from Telegram: {Update}", update.Type);

		if (update is not { Message.From.Username: not null })
		{
			return;
		}

		switch (update.Message.Text)
		{
			case "/start":
				if (_options.Value.AllowedUserNames.Contains(update.Message.From.Username))
				{
					var userId = update.Message.From.Id;
					var username = update.Message.From.Username;
					var chatId = update.Message.Chat.Id;
					if (await RegisterChat(userId, username, chatId))
					{
						await client.SendTextMessageAsync(chatId, "Welcome to TgHomeBot. You can leave with /end.", cancellationToken: cancellationToken);
					}
				}
				break;
			case "/end":
				if (update.Message is not null)
				{
					if (await UnregisterChatAsync(update.Message.Chat.Id))
					{
						await client.SendTextMessageAsync(update.Message.Chat.Id, "Bye from TgHomeBot", cancellationToken: cancellationToken);
					}
				}
				break;
			default:
				_logger.LogDebug("Received unknown message: {Message}", update.Message.Text);
				break;
		}
	}

	private async Task<bool> RegisterChat(long userId, string username, long chatId)
	{
		var existingChat = _registeredChats.FirstOrDefault(r => r.ChatId == chatId);
		if (existingChat is not null)
		{
			return false;
		}

		_registeredChats.Add(new RegisteredChat
		{
			Id = userId,
			Username = username,
			ChatId = chatId
		});

		await SaveRegisteredChats();
		_logger.LogInformation("Registered chat {ChatId} with user {User}", chatId, username);

		return true;
	}

	private async Task<bool> UnregisterChatAsync(long chatId)
	{
		var existingChat = _registeredChats.FirstOrDefault(r => r.ChatId == chatId);
		if (existingChat is null)
		{
			return false;
		}

		_registeredChats.Remove(existingChat);
		await SaveRegisteredChats();
		return true;

	}

	public async Task SendAsync(string message)
	{
		foreach (var registeredChat in _registeredChats)
		{
			await _botClient.SendTextMessageAsync(registeredChat.ChatId, message, parseMode: ParseMode.Html);

			_logger.LogInformation("Message sent to chat {ChatId} with user {User}: {Message}", registeredChat.ChatId, registeredChat.Username, message);
		}
	}

	private async Task SaveRegisteredChats()
	{
		var json = JsonSerializer.Serialize(_registeredChats, _jsonSerializerOptions);
		await File.WriteAllTextAsync(Path.Combine(_fileStorageOptions.Value.Path, "RegisteredChats.json"), json, Encoding.UTF8);
	}

	private async Task LoadRegisteredChats()
	{
		var filename = Path.Combine(_fileStorageOptions.Value.Path, "RegisteredChats.json");
		if (File.Exists(filename))
		{
			var json = await File.ReadAllTextAsync(filename, Encoding.UTF8);
			var registeredChats = JsonSerializer.Deserialize<RegisteredChat[]>(json)!;
			_registeredChats.AddRange(registeredChats);
		}
	}
}
