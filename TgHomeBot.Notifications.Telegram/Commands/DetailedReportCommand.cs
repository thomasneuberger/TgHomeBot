using System.Globalization;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgHomeBot.Charging.Contract.Requests;
using TgHomeBot.Charging.Contract.Services;
using TgHomeBot.Common.Contract;

namespace TgHomeBot.Notifications.Telegram.Commands;

internal class DetailedReportCommand(
    IServiceProvider serviceProvider,
    IOptions<ApplicationOptions> applicationOptions,
    IOptions<FileStorageOptions> fileStorageOptions,
    IDetailedReportCsvGenerator csvGenerator,
    ILogger<DetailedReportCommand> logger) : ICommand
{
    private const int MaxTelegramMessageLength = 4000;
    private readonly ApplicationOptions _applicationOptions = applicationOptions.Value;
    private readonly FileStorageOptions _fileStorageOptions = fileStorageOptions.Value;

    public string Name => "/detailedreport";

    public string Description => "Detaillierter Bericht aller Ladevorgänge";

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
            await client.SendMessage(new ChatId(message.Chat.Id),
                $"❌ Fehler beim Abrufen der Ladevorgänge:\n{result.ErrorMessage}",
                cancellationToken: cancellationToken);
            
            // If it's an authentication error, send the login URL as a separate message
            if (result.ErrorMessage?.Contains("Nicht mit Easee API authentifiziert") == true)
            {
                var loginUrl = $"{_applicationOptions.BaseUrl.TrimEnd('/')}/Easee/Login";
                await client.SendMessage(new ChatId(message.Chat.Id),
                    loginUrl,
                    cancellationToken: cancellationToken);
            }
            return;
        }

        var sessions = result.Data!;

        if (sessions.Count == 0)
        {
            await client.SendMessage(new ChatId(message.Chat.Id),
                "Keine Ladevorgänge in den letzten zwei Monaten gefunden.",
                cancellationToken: cancellationToken);
            return;
        }

        // Order by user name and then by car connection timestamp
        var orderedSessions = sessions
            .OrderBy(s => s.UserName)
            .ThenBy(s => s.CarConnected)
            .ToList();

        var reportLines = new List<string> { "📋 Detaillierter Ladebericht (letzte 2 Monate):" };

        string? currentUserName = null;

        foreach (var session in orderedSessions)
        {
            // Add user header if it's a new user
            if (currentUserName != session.UserName)
            {
                if (currentUserName != null)
                {
                    reportLines.Add(""); // Empty line between users
                }
                currentUserName = session.UserName;
                reportLines.Add($"👤 Benutzer: {session.UserName}");
            }

            var connectedTime = session.CarConnected.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
            var duration = FormatDuration(session.ActualDurationSeconds);
            
            reportLines.Add($"  🔌 {connectedTime} | {session.KiloWattHours:F2} kWh | {duration}");
        }

        var report = string.Join('\n', reportLines);

        // Generate CSV files for each month
        var csvFiles = await GenerateCsvFilesAsync(sessions, cancellationToken);

        // Split the message if it's too long (Telegram has a 4096 character limit)
        if (report.Length <= MaxTelegramMessageLength)
        {
            await client.SendMessage(new ChatId(message.Chat.Id), report, cancellationToken: cancellationToken);
        }
        else
        {
            // Split by lines and send multiple messages
            var messages = SplitIntoMessages(reportLines, MaxTelegramMessageLength);
            foreach (var msg in messages)
            {
                await client.SendMessage(new ChatId(message.Chat.Id), msg, cancellationToken: cancellationToken);
            }
        }

        // Send CSV files
        foreach (var csvFile in csvFiles)
        {
            using var stream = new MemoryStream(csvFile.Data);
            var inputFile = new InputFileStream(stream, csvFile.FileName);
            await client.SendDocument(new ChatId(message.Chat.Id), inputFile, cancellationToken: cancellationToken);
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

    private async Task<List<CsvFileData>> GenerateCsvFilesAsync(IReadOnlyList<Charging.Contract.Models.ChargingSession> sessions, CancellationToken cancellationToken)
    {
        var csvFiles = new List<CsvFileData>();
        
        // Group sessions by month
        var monthlyGroups = sessions
            .GroupBy(s => new { s.CarConnected.Year, s.CarConnected.Month })
            .OrderBy(g => g.Key.Year)
            .ThenBy(g => g.Key.Month)
            .ToList();

        var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        foreach (var group in monthlyGroups)
        {
            var monthDate = new DateTime(group.Key.Year, group.Key.Month, 1);
            var monthSessions = group.ToList();

            var csvData = csvGenerator.GenerateMonthlyCsv(monthSessions, group.Key.Year, group.Key.Month);
            var fileName = csvGenerator.GetFileName(group.Key.Year, group.Key.Month);
            
            csvFiles.Add(new CsvFileData { FileName = fileName, Data = csvData });

            // If the month has already ended, save the CSV to file storage
            if (monthDate < currentMonth)
            {
                await SaveCsvToStorageAsync(fileName, csvData, cancellationToken);
            }
        }

        return csvFiles;
    }

    private async Task SaveCsvToStorageAsync(string fileName, byte[] csvData, CancellationToken cancellationToken)
    {
        try
        {
            var directory = Path.Combine(_fileStorageOptions.Path, "monthly-reports");
            Directory.CreateDirectory(directory);
            
            var filePath = Path.Combine(directory, fileName);
            await File.WriteAllBytesAsync(filePath, csvData, cancellationToken);
            
            logger.LogInformation("Saved CSV report to {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save CSV report {FileName} to storage", fileName);
        }
    }

    private class CsvFileData
    {
        public required string FileName { get; init; }
        public required byte[] Data { get; init; }
    }
}
