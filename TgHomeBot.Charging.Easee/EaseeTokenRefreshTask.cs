using Microsoft.Extensions.Logging;
using TgHomeBot.Charging.Contract;
using TgHomeBot.Scheduling;

namespace TgHomeBot.Charging.Easee;

/// <summary>
/// Scheduled task to refresh Easee authentication token
/// Runs every 30 minutes to keep the token valid
/// </summary>
public class EaseeTokenRefreshTask : IScheduledTask
{
    private readonly ILogger<EaseeTokenRefreshTask> _logger;
    private readonly IChargingConnector _chargingConnector;

    public string TaskName => "EaseeTokenRefreshTask";

    public EaseeTokenRefreshTask(
        ILogger<EaseeTokenRefreshTask> logger,
        IChargingConnector chargingConnector)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _chargingConnector = chargingConnector ?? throw new ArgumentNullException(nameof(chargingConnector));
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Easee token refresh task");

        if (!_chargingConnector.IsAuthenticated)
        {
            _logger.LogWarning("Not authenticated with Easee API, skipping token refresh");
            return;
        }

        var success = await _chargingConnector.RefreshTokenAsync(cancellationToken);
        
        if (success)
        {
            _logger.LogInformation("Successfully refreshed Easee token");
        }
        else
        {
            _logger.LogError("Failed to refresh Easee token");
        }
    }
}
