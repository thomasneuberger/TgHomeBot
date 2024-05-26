using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgHomeBot.Notifications.Telegram.Services;

namespace TgHomeBot.Notifications.Telegram.Commands;

internal class StartCommand(IOptions<TelegramOptions> options, IRegisteredChatService registeredChatService) : ICommand
{
    public bool AllowUnregistered => true;

    public string Name => "/start";

    public string Description => "Eine Verbindung zum Bot herstellen";

    public async Task ProcessMessage(Message message, ITelegramBotClient client, CancellationToken cancellationToken)
    {
        if (message.From?.Username is null)
        {
            return;
        }

        if (!options.Value.AllowedUserNames.Contains(message.From.Username))
        {
            return;
        }

        var userId = message.From.Id;
        var username = message.From.Username;
        var chatId = message.Chat.Id;
        if (await registeredChatService.RegisterChat(userId, username, chatId))
        {
            await client.SendTextMessageAsync(chatId,
                "Willkommen zu TgHomeBot. Du kannst die Verbindung mit /end trennen.",
                cancellationToken: cancellationToken);
        }
    }
}
