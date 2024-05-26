using Telegram.Bot;
using Telegram.Bot.Types;

namespace TgHomeBot.Notifications.Telegram.Commands;

internal class HelpCommand : ICommand
{
    public string Name => "/help";
    public string Description => "Informationen zur Benutzung";
    public async Task ProcessMessage(Message message, ITelegramBotClient client, CancellationToken cancellationToken)
    {
        var helpMessage = """
                      Der TgHomeBot überwacht Geräte im Smart Home und schickt eine Nachricht, wenn eines davon seine Arbeit beendet hat, d.h. die Leistungsaufnahme unter einen bestimmten Schwellwert fällt.
                      /start: Eine Verbindung mit TgHomeBot herstellen. Die Verbdindung wird nur für bestimmte Benutzer hergestellt.
                      /check: Prüfen, ob eine Verbindung mit TgHomeBot  hergestellt ist.
                      /devices: Gibt die aktuelle Leistungsaufnahme der überwachten Geräte aus.
                      /end: Trennt die Verbindung zum TghomeBot. Die Verbindung kann mit /start wieder hergestellt werden.
                      /help: Gibt diese Hilfeinformationen aus.
                      """;

        await client.SendTextMessageAsync(new ChatId(message.Chat.Id), helpMessage, cancellationToken: cancellationToken);
    }
}
