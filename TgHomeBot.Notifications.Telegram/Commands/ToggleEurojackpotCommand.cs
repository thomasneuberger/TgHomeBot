using Telegram.Bot;
using Telegram.Bot.Types;
using TgHomeBot.Notifications.Telegram.Services;

namespace TgHomeBot.Notifications.Telegram.Commands;

internal class ToggleEurojackpotCommand(IRegisteredChatService registeredChatService) : ICommand
{
    public string Name => "/toggle_eurojackpot";

    public string Description => "Eurojackpot Benachrichtigungen ein-/ausschalten";

    public async Task ProcessMessage(Message message, ITelegramBotClient client, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var success = await registeredChatService.ToggleEurojackpotAsync(chatId);
        
        if (!success)
        {
            await client.SendTextMessageAsync(chatId,
                "❌ Fehler: Chat ist nicht registriert.",
                cancellationToken: cancellationToken);
            return;
        }

        var chat = registeredChatService.GetRegisteredChat(chatId);
        var status = chat!.EurojackpotEnabled ? "aktiviert ✅" : "deaktiviert ❌";
        
        await client.SendTextMessageAsync(chatId,
            $"Eurojackpot Benachrichtigungen wurden {status}",
            cancellationToken: cancellationToken);
    }
}
