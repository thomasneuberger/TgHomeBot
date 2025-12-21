using AsyncAwaitBestPractices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TgHomeBot.Common.Contract;
using TgHomeBot.SmartHome.Contract;
using TgHomeBot.SmartHome.Contract.Models;

namespace TgHomeBot.Api;

public class MonitoringService(
    IServiceProvider services, 
    IOptions<SmartHomeOptions> options, 
    ILogger<MonitoringService> logger) : IHostedService
{
    private ISmartHomeMonitor? _smartHomeMonitor;

    public MonitorState GetState()
    {
        return _smartHomeMonitor?.State ?? MonitorState.Unknown;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var smartHomeConnector = services.GetRequiredService<ISmartHomeConnector>();
        _smartHomeMonitor = await smartHomeConnector.CreateMonitorAsync(options.Value.MonitoredDevices, cancellationToken);

        // Start monitoring in the background to avoid blocking application startup
        _smartHomeMonitor.StartMonitoringAsync(cancellationToken).SafeFireAndForget(onException: ex => 
            logger.LogError(ex, "Unhandled error in background monitoring task"));
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_smartHomeMonitor is not null)
        {
            await _smartHomeMonitor.StopMonitoring(cancellationToken);
        }
    }
}