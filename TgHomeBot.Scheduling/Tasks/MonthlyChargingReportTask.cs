using Microsoft.Extensions.Logging;
using System.Globalization;
using MediatR;
using TgHomeBot.Charging.Contract.Requests;
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

    public string TaskName => "MonthlyChargingReportTask";

    public MonthlyChargingReportTask(
        ILogger<MonthlyChargingReportTask> logger,
        INotificationConnector notificationConnector,
        IMediator mediator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notificationConnector = notificationConnector ?? throw new ArgumentNullException(nameof(notificationConnector));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
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

            // Group by user and month, then sum the energy
            var monthlyReport = sessions
                .GroupBy(s => new { s.UserName, Year = s.CarConnected.Year, Month = s.CarConnected.Month })
                .Select(g => new
                {
                    g.Key.UserName,
                    g.Key.Year,
                    g.Key.Month,
                    TotalKwh = g.Sum(s => s.KiloWattHours)
                })
                .OrderBy(x => x.UserName)
                .ThenBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToList();

            var reportLines = new List<string> { "📊 Monatlicher Ladebericht (letzte 2 Monate):" };

            foreach (var entry in monthlyReport)
            {
                var monthName = new DateTime(entry.Year, entry.Month, 1).ToString("MMMM yyyy", CultureInfo.GetCultureInfo("de-DE"));
                reportLines.Add($"👤 {entry.UserName} - {monthName}: {entry.TotalKwh:F2} kWh");
            }

            var report = string.Join('\n', reportLines);

            await _notificationConnector.SendAsync(report, NotificationType.MonthlyChargingReport);
            _logger.LogInformation("Successfully sent monthly charging report");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing monthly charging report task");
        }
    }
}
