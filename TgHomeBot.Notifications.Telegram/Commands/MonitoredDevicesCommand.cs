using System.Text.Json;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgHomeBot.Common.Contract;

namespace TgHomeBot.Notifications.Telegram.Commands;

internal class MonitoredDevicesCommand(IOptions<SmartHomeOptions> options) : ICommand
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerOptions.Default)
    {
        WriteIndented = true
    };

    public string Name => "/monitored";

    public string Description => "Die überwachten Geräte und ihre Einstellungen abfragen";

    public async Task ProcessMessage(Message message, ITelegramBotClient client, CancellationToken cancellationToken)
    {
        var monitoredDevices = JsonSerializer.Serialize(options.Value.MonitoredDevices, JsonOptions);

        await client.SendTextMessageAsync(new ChatId(message.Chat.Id), monitoredDevices, cancellationToken: cancellationToken);
    }
}
