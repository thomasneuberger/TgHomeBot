using Telegram.Bot;
using Telegram.Bot.Types;
using TgHomeBot.Notifications.Telegram.Services;

namespace TgHomeBot.Notifications.Telegram.Commands;

internal class ToggleDeviceNotificationsCommand(IRegisteredChatService registeredChatService) : ICommand
{
    public string Name => "/toggle_devicenotifications";

    public string Description => "Gerätebenachrichtigungen ein-/ausschalten";

    public async Task ProcessMessage(Message message, ITelegramBotClient client, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var success = await registeredChatService.ToggleDeviceNotificationsAsync(chatId);
        
        if (!success)
        {
            await client.SendMessage(chatId,
                "❌ Fehler: Chat ist nicht registriert.",
                cancellationToken: cancellationToken);
            return;
        }

        var chat = registeredChatService.GetRegisteredChat(chatId);
        var status = chat!.DeviceNotificationsEnabled ? "aktiviert ✅" : "deaktiviert ❌";
        
        await client.SendMessage(chatId,
            $"Gerätebenachrichtigungen wurden {status}",
            cancellationToken: cancellationToken);
    }
}
