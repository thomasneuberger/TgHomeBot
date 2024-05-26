using Telegram.Bot;
using Telegram.Bot.Types;
using TgHomeBot.Notifications.Telegram.Services;

namespace TgHomeBot.Notifications.Telegram.Commands;

internal class CheckCommand(IRegisteredChatService registeredChatService) : ICommand
{
    public bool AllowUnregistered => true;

    public string Name => "/check";

    public string Description => "Prüfen ob eie Verbindung mit dem Bot besteht.";

    public async Task ProcessMessage(Message message, ITelegramBotClient client, CancellationToken cancellationToken)
    {
        var isRegistered = registeredChatService.RegisteredChats.Any(c => c.ChatId == message.Chat.Id);

        if (isRegistered)
        {
            await client.SendTextMessageAsync(new ChatId(message.Chat.Id), "Es besteht eine Verbindung zum TgHomeBot", cancellationToken: cancellationToken);
        }
        else
        {
            await client.SendTextMessageAsync(new ChatId(message.Chat.Id), "Es besteht keine Verbindung zum TgHomeBot", cancellationToken: cancellationToken);
        }
    }
}
