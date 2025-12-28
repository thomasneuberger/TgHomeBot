using Telegram.Bot;
using Telegram.Bot.Types;
using TgHomeBot.Notifications.Telegram.Services;

namespace TgHomeBot.Notifications.Telegram.Commands;

internal class ToggleMonthlyReportCommand(IRegisteredChatService registeredChatService) : ICommand
{
    public string Name => "/togglemonthlyreport";

    public string Description => "Monatliche Ladeberichte ein-/ausschalten";

    public async Task ProcessMessage(Message message, ITelegramBotClient client, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var success = await registeredChatService.ToggleMonthlyChargingReportAsync(chatId);
        
        if (!success)
        {
            await client.SendTextMessageAsync(chatId,
                "❌ Fehler: Chat ist nicht registriert.",
                cancellationToken: cancellationToken);
            return;
        }

        var chat = registeredChatService.GetRegisteredChat(chatId);
        var status = chat!.MonthlyChargingReportEnabled ? "aktiviert ✅" : "deaktiviert ❌";
        
        await client.SendTextMessageAsync(chatId,
            $"Monatliche Ladeberichte wurden {status}",
            cancellationToken: cancellationToken);
    }
}
