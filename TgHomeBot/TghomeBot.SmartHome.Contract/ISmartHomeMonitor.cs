using TgHomeBot.SmartHome.Contract.Models;

namespace TgHomeBot.SmartHome.Contract;

public interface ISmartHomeMonitor : IDisposable
{
    Task StartMonitoringAsync(CancellationToken cancellationToken);
    Task StopMonitoring(CancellationToken cancellationToken);
    MonitorState State { get; }
}