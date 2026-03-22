using Microsoft.Extensions.Logging;
using TgHomeBot.SmartHome.Contract;

namespace TgHomeBot.Scheduling.Tasks;

/// <summary>
/// Scheduled task to ensure the Smart Home Monitor is running
/// Periodically checks and restarts the monitor if needed
/// </summary>
public class SmartHomeMonitorWatcherTask(ILogger<SmartHomeMonitorWatcherTask> logger, ISmartHomeConnector smartHomeConnector)
    : IScheduledTask
{
    public string TaskName => "SmartHomeMonitorWatcherTask";

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var isRunning = await smartHomeConnector.EnsureMonitorIsRunning();
        logger.LogInformation("Smart Home Monitor is running: {IsRunning}", isRunning);
    }
}
