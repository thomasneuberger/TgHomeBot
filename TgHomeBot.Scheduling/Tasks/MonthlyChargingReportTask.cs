using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MediatR;
using TgHomeBot.Charging.Contract.Requests;
using TgHomeBot.Charging.Contract.Services;
using TgHomeBot.Common.Contract;
using TgHomeBot.Notifications.Contract;
using TgHomeBot.Notifications.Contract.Requests;

namespace TgHomeBot.Scheduling.Tasks;

/// <summary>
/// Scheduled task to send monthly charging report to Telegram
/// </summary>
public class MonthlyChargingReportTask : IScheduledTask
{
    private readonly ILogger<MonthlyChargingReportTask> _logger;
    private readonly INotificationConnector _notificationConnector;
    private readonly IMediator _mediator;
    private readonly IMonthlyReportFormatter _formatter;
    private readonly IMonthlyReportPdfGenerator _pdfGenerator;
    private readonly FileStorageOptions _fileStorageOptions;

    public string TaskName => "MonthlyChargingReportTask";

    public MonthlyChargingReportTask(
        ILogger<MonthlyChargingReportTask> logger,
        INotificationConnector notificationConnector,
        IMediator mediator,
        IMonthlyReportFormatter formatter,
        IMonthlyReportPdfGenerator pdfGenerator,
        IOptions<FileStorageOptions> fileStorageOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notificationConnector = notificationConnector ?? throw new ArgumentNullException(nameof(notificationConnector));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        _pdfGenerator = pdfGenerator ?? throw new ArgumentNullException(nameof(pdfGenerator));
        _fileStorageOptions = fileStorageOptions?.Value ?? throw new ArgumentNullException(nameof(fileStorageOptions));
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting monthly charging report task");

        try
        {
            // Get sessions for the last two months
            var to = DateTime.UtcNow.Date;
            var from = new DateTime(to.Year, to.Month, 1).AddMonths(-2); // First day of 2 months ago

            var result = await _mediator.Send(new GetChargingSessionsRequest(from, to), cancellationToken);

            if (!result.Success)
            {
                _logger.LogError("Failed to fetch charging sessions: {ErrorMessage}", result.ErrorMessage);
                return;
            }

            var sessions = result.Data!;

            if (sessions.Count == 0)
            {
                _logger.LogInformation("No charging sessions found in the last two months");
                return;
            }

            var report = _formatter.FormatMonthlyReport(sessions);

            // Generate overview PDF for the entire time range
            var overviewPdfData = _pdfGenerator.GenerateOverviewPdf(sessions);
            var overviewFileName = _pdfGenerator.GetOverviewFileName();

            var pdfFiles = new List<FileAttachment>
            {
                new FileAttachment { FileName = overviewFileName, Data = overviewPdfData }
            };

            // Generate PDFs for each month that has data
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

                var pdfData = _pdfGenerator.GenerateMonthlyPdf(monthSessions, group.Key.Year, group.Key.Month);
                var fileName = _pdfGenerator.GetFileName(group.Key.Year, group.Key.Month);
                
                pdfFiles.Add(new FileAttachment { FileName = fileName, Data = pdfData });

                // If the month has already ended, save the PDF to file storage
                if (monthDate < currentMonth)
                {
                    await SavePdfToStorage(fileName, pdfData);
                }
            }

            // Send report with PDF attachments (overview first, then monthly reports)
            await _notificationConnector.SendWithFilesAsync(report, pdfFiles, NotificationType.MonthlyChargingReport);
            
            _logger.LogInformation("Successfully sent monthly charging report with {PdfCount} PDF files", pdfFiles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing monthly charging report task");
        }
    }

    private async Task SavePdfToStorage(string fileName, byte[] pdfData)
    {
        try
        {
            var directory = Path.Combine(_fileStorageOptions.Path, "monthly-reports");
            Directory.CreateDirectory(directory);
            
            var filePath = Path.Combine(directory, fileName);
            await File.WriteAllBytesAsync(filePath, pdfData);
            
            _logger.LogInformation("Saved PDF report to {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save PDF report {FileName} to storage", fileName);
        }
    }
}
