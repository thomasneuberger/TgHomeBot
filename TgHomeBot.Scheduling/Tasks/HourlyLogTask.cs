using Microsoft.Extensions.Logging;

namespace TgHomeBot.Scheduling.Tasks;

/// <summary>
/// A demonstration task that logs the current date and time
/// Designed to run every full hour
/// </summary>
public class HourlyLogTask : IScheduledTask
{
    private readonly ILogger<HourlyLogTask> _logger;

    public string TaskName => "HourlyLogTask";

    public HourlyLogTask(ILogger<HourlyLogTask> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var currentTime = DateTime.UtcNow;
        _logger.LogInformation("HourlyLogTask executed at: {DateTime}", currentTime.ToString("yyyy-MM-dd HH:mm:ss"));
        return Task.CompletedTask;
    }
}
