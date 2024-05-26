using Microsoft.Extensions.Options;
using TgHomeBot.Common.Contract;
using TgHomeBot.SmartHome.Contract;
using TgHomeBot.SmartHome.Contract.Models;

namespace TgHomeBot.Api;

public class MonitoringService(IServiceProvider services, IOptions<SmartHomeOptions> options) : IHostedService
{
    private ISmartHomeMonitor? _smartHomeMonitor;

    public MonitorState GetState()
    {
        return _smartHomeMonitor?.State ?? MonitorState.Unknown;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var smartHomeConnector = scope.ServiceProvider.GetRequiredService<ISmartHomeConnector>();
        _smartHomeMonitor = smartHomeConnector.CreateMonitorAsync(options.Value.MonitoredDevices, cancellationToken);

        await _smartHomeMonitor.StartMonitoringAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_smartHomeMonitor is not null)
        {
            await _smartHomeMonitor.StopMonitoring(cancellationToken);
        }
    }
}