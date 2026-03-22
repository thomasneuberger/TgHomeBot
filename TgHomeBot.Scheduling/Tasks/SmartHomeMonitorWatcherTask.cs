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
        cancellationToken.ThrowIfCancellationRequested();

        var isRunning = await smartHomeConnector.EnsureMonitorIsRunning(cancellationToken).ConfigureAwait(false);

        if (!isRunning)
        {
            logger.LogWarning("Smart Home Monitor is not running and could not be started.");
        }
        else
        {
            logger.LogDebug("Smart Home Monitor is running.");
        }
    }
}
