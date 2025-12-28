using Microsoft.Extensions.Logging;
using MediatR;
using TgHomeBot.Charging.Contract.Requests;
using TgHomeBot.Charging.Contract.Services;
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

    public string TaskName => "MonthlyChargingReportTask";

    public MonthlyChargingReportTask(
        ILogger<MonthlyChargingReportTask> logger,
        INotificationConnector notificationConnector,
        IMediator mediator,
        IMonthlyReportFormatter formatter)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notificationConnector = notificationConnector ?? throw new ArgumentNullException(nameof(notificationConnector));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
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

            await _notificationConnector.SendAsync(report, NotificationType.MonthlyChargingReport);
            _logger.LogInformation("Successfully sent monthly charging report");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing monthly charging report task");
        }
    }
}
