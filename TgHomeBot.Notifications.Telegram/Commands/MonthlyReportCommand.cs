using System.Globalization;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgHomeBot.Charging.Contract.Requests;

namespace TgHomeBot.Notifications.Telegram.Commands;

internal class MonthlyReportCommand(IServiceProvider serviceProvider) : ICommand
{
    public string Name => "/monthlyreport";

    public string Description => "Monatliche Zusammenfassung des geladenen Stroms";

    public async Task ProcessMessage(Message message, ITelegramBotClient client, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Get sessions for the last two months
        var to = DateTime.UtcNow.Date;
        var from = new DateTime(to.Year, to.Month, 1).AddMonths(-2); // First day of 2 months ago

        var result = await mediator.Send(new GetChargingSessionsRequest(from, to), cancellationToken);

        if (!result.Success)
        {
            await client.SendTextMessageAsync(new ChatId(message.Chat.Id),
                $"❌ Fehler beim Abrufen der Ladevorgänge:\n{result.ErrorMessage}",
                cancellationToken: cancellationToken);
            return;
        }

        var sessions = result.Data!;
        
        if (sessions.Count == 0)
        {
            await client.SendTextMessageAsync(new ChatId(message.Chat.Id),
                "Keine Ladevorgänge in den letzten zwei Monaten gefunden.",
                cancellationToken: cancellationToken);
            return;
        }

        // Group by user and month, then sum the energy
        var monthlyReport = sessions
            .GroupBy(s => new { s.UserId, Year = s.CarConnected.Year, Month = s.CarConnected.Month })
            .Select(g => new
            {
                g.Key.UserId,
                g.Key.Year,
                g.Key.Month,
                TotalKwh = g.Sum(s => s.KiloWattHours)
            })
            .OrderBy(x => x.UserId)
            .ThenBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToList();

        var reportLines = new List<string> { "📊 Monatlicher Ladebericht (letzte 2 Monate):" };

        foreach (var entry in monthlyReport)
        {
            var monthName = new DateTime(entry.Year, entry.Month, 1).ToString("MMMM yyyy", CultureInfo.GetCultureInfo("de-DE"));
            reportLines.Add($"👤 {entry.UserId} - {monthName}: {entry.TotalKwh:F2} kWh");
        }

        var report = string.Join('\n', reportLines);

        await client.SendTextMessageAsync(new ChatId(message.Chat.Id), report, cancellationToken: cancellationToken);
    }
}
