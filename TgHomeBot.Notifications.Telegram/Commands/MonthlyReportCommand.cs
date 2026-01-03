using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgHomeBot.Charging.Contract.Requests;
using TgHomeBot.Charging.Contract.Services;
using TgHomeBot.Common.Contract;
using TgHomeBot.Notifications.Contract.Requests;

namespace TgHomeBot.Notifications.Telegram.Commands;

internal class MonthlyReportCommand(IServiceProvider serviceProvider, IOptions<ApplicationOptions> applicationOptions) : ICommand
{
    private readonly ApplicationOptions _applicationOptions = applicationOptions.Value;

    public string Name => "/monthlyreport";

    public string Description => "Monatliche Zusammenfassung des geladenen Stroms";

    public async Task ProcessMessage(Message message, ITelegramBotClient client, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var formatter = scope.ServiceProvider.GetRequiredService<IMonthlyReportFormatter>();
        var pdfGenerator = scope.ServiceProvider.GetRequiredService<IMonthlyReportPdfGenerator>();
        var fileStorageOptions = scope.ServiceProvider.GetRequiredService<IOptions<FileStorageOptions>>();

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
        var report = formatter.FormatMonthlyReport(sessions);

        await client.SendMessage(new ChatId(message.Chat.Id), report, cancellationToken: cancellationToken);

        // Generate and send overview PDF first
        var overviewPdfData = pdfGenerator.GenerateOverviewPdf(sessions);
        var overviewFileName = pdfGenerator.GetOverviewFileName();
        
        using (var overviewStream = new MemoryStream(overviewPdfData))
        {
            var overviewFile = new InputFileStream(overviewStream, overviewFileName);
            await client.SendDocument(new ChatId(message.Chat.Id), overviewFile, cancellationToken: cancellationToken);
        }

        // Generate and send PDFs for each month that has data
        var monthlyGroups = sessions
            .GroupBy(s => new { s.CarConnected.Year, s.CarConnected.Month })
            .OrderBy(g => g.Key.Year)
            .ThenBy(g => g.Key.Month)
            .ToList();

        var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        foreach (var group in monthlyGroups)
        {
            var monthSessions = group.ToList();
            var monthDate = new DateTime(group.Key.Year, group.Key.Month, 1);

            var pdfData = pdfGenerator.GenerateMonthlyPdf(monthSessions, group.Key.Year, group.Key.Month);
            var fileName = pdfGenerator.GetFileName(group.Key.Year, group.Key.Month);
            
            // Save monthly PDF if the month has already ended
            if (monthDate < currentMonth)
            {
                await SavePdfToStorage(fileStorageOptions.Value, fileName, pdfData);
            }
            
            using var stream = new MemoryStream(pdfData);
            var inputFile = new InputFileStream(stream, fileName);
            await client.SendDocument(new ChatId(message.Chat.Id), inputFile, cancellationToken: cancellationToken);
        }
    }

    private static async Task SavePdfToStorage(FileStorageOptions fileStorageOptions, string fileName, byte[] pdfData)
    {
        try
        {
            var directory = Path.Combine(fileStorageOptions.Path, "monthly-reports");
            Directory.CreateDirectory(directory);
            
            var filePath = Path.Combine(directory, fileName);
            await File.WriteAllBytesAsync(filePath, pdfData);
        }
        catch
        {
            // Silently ignore storage errors in command context
        }
    }
}
