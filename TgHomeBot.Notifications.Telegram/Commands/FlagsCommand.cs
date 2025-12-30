using Telegram.Bot;
using Telegram.Bot.Types;
using TgHomeBot.Notifications.Telegram.Services;

namespace TgHomeBot.Notifications.Telegram.Commands;

internal class FlagsCommand(IRegisteredChatService registeredChatService) : ICommand
{
    public string Name => "/flags";

    public string Description => "Zeige den Status der Feature-Flags";

    public async Task ProcessMessage(Message message, ITelegramBotClient client, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var chat = registeredChatService.GetRegisteredChat(chatId);
        
        if (chat is null)
        {
            await client.SendMessage(chatId,
                "❌ Fehler: Chat ist nicht registriert.",
                cancellationToken: cancellationToken);
            return;
        }

        var flagsMessage = $"<b>Feature-Flags für diesen Chat:</b>\n\n" +
                          $"🎰 <b>Eurojackpot:</b> {GetStatusEmoji(chat.EurojackpotEnabled)} {GetStatusText(chat.EurojackpotEnabled)}\n" +
                          $"⚡ <b>Monatlicher Ladebericht:</b> {GetStatusEmoji(chat.MonthlyChargingReportEnabled)} {GetStatusText(chat.MonthlyChargingReportEnabled)}\n" +
                          $"🏠 <b>Gerätebenachrichtigungen:</b> {GetStatusEmoji(chat.DeviceNotificationsEnabled)} {GetStatusText(chat.DeviceNotificationsEnabled)}\n\n" +
                          $"<i>Verwende /toggle_[flagname] um einen Flag zu ändern.</i>";
        
        await client.SendMessage(chatId,
            flagsMessage,
            parseMode: global::Telegram.Bot.Types.Enums.ParseMode.Html,
            cancellationToken: cancellationToken);
    }

    private static string GetStatusEmoji(bool enabled) => enabled ? "✅" : "❌";
    private static string GetStatusText(bool enabled) => enabled ? "Aktiviert" : "Deaktiviert";
}
