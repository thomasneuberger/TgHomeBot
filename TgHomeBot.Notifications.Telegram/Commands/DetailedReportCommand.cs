using System.Globalization;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgHomeBot.Charging.Contract.Requests;

namespace TgHomeBot.Notifications.Telegram.Commands;

internal class DetailedReportCommand(IServiceProvider serviceProvider) : ICommand
{
    public string Name => "/detailedreport";

    public string Description => "Detaillierter Bericht aller Ladevorgänge";

    public async Task ProcessMessage(Message message, ITelegramBotClient client, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Get sessions for the last two months
        var to = DateTime.UtcNow.Date;
        var from = new DateTime(to.Year, to.Month, 1).AddMonths(-2); // First day of 2 months ago

        var sessions = await mediator.Send(new GetChargingSessionsRequest(from, to), cancellationToken);

        if (sessions.Count == 0)
        {
            await client.SendTextMessageAsync(new ChatId(message.Chat.Id),
                "Keine Ladevorgänge in den letzten zwei Monaten gefunden.",
                cancellationToken: cancellationToken);
            return;
        }

        // Order by user id and then by car connection timestamp
        var orderedSessions = sessions
            .OrderBy(s => s.UserId)
            .ThenBy(s => s.CarConnected)
            .ToList();

        var reportLines = new List<string> { "📋 Detaillierter Ladebericht (letzte 2 Monate):\n" };

        string? currentUserId = null;

        foreach (var session in orderedSessions)
        {
            // Add user header if it's a new user
            if (currentUserId != session.UserId)
            {
                if (currentUserId != null)
                {
                    reportLines.Add(""); // Empty line between users
                }
                currentUserId = session.UserId;
                reportLines.Add($"👤 Benutzer: {session.UserId}");
            }

            var connectedTime = session.CarConnected.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
            var duration = FormatDuration(session.ActualDurationSeconds);
            
            reportLines.Add($"  🔌 {connectedTime} | {session.KiloWattHours:F2} kWh | {duration}");
        }

        var report = string.Join('\n', reportLines);

        // Split the message if it's too long (Telegram has a 4096 character limit)
        const int maxMessageLength = 4000;
        if (report.Length <= maxMessageLength)
        {
            await client.SendTextMessageAsync(new ChatId(message.Chat.Id), report, cancellationToken: cancellationToken);
        }
        else
        {
            // Split by lines and send multiple messages
            var messages = SplitIntoMessages(reportLines, maxMessageLength);
            foreach (var msg in messages)
            {
                await client.SendTextMessageAsync(new ChatId(message.Chat.Id), msg, cancellationToken: cancellationToken);
            }
        }
    }

    private static string FormatDuration(int? durationSeconds)
    {
        if (durationSeconds == null || durationSeconds == 0)
        {
            return "Dauer unbekannt";
        }

        var totalMinutes = durationSeconds.Value / 60;
        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;

        if (hours > 0)
        {
            return $"{hours}h {minutes}min";
        }
        return $"{minutes}min";
    }

    private static List<string> SplitIntoMessages(List<string> lines, int maxLength)
    {
        var messages = new List<string>();
        var currentMessage = new List<string>();
        var currentLength = 0;

        foreach (var line in lines)
        {
            var lineLength = line.Length + 1; // +1 for newline

            if (currentLength + lineLength > maxLength && currentMessage.Count > 0)
            {
                // Start a new message
                messages.Add(string.Join('\n', currentMessage));
                currentMessage.Clear();
                currentLength = 0;
            }

            currentMessage.Add(line);
            currentLength += lineLength;
        }

        if (currentMessage.Count > 0)
        {
            messages.Add(string.Join('\n', currentMessage));
        }

        return messages;
    }
}
