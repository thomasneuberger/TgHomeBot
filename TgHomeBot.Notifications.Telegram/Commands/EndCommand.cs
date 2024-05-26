using Telegram.Bot;
using Telegram.Bot.Types;
using TgHomeBot.Notifications.Telegram.Services;

namespace TgHomeBot.Notifications.Telegram.Commands;
internal class EndCommand(IRegisteredChatService registeredChatService) : ICommand
{
    public string Name => "/end";
    public string Description => "Die Verbindung zum Bot trennen";
    public async Task ProcessMessage(Message message, ITelegramBotClient client, CancellationToken cancellationToken)
    {
        if (await registeredChatService.UnregisterChatAsync(message.Chat.Id))
        {
            await client.SendTextMessageAsync(message.Chat.Id, "Auf Wiedersehen. Du kannst die Verbindung mit /start wieder herstellen.", cancellationToken: cancellationToken);
        }
    }
}
