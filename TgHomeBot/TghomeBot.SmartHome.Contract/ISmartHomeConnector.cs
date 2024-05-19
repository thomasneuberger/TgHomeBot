using TgHomeBot.SmartHome.Contract.Models;

namespace TgHomeBot.SmartHome.Contract;

public interface ISmartHomeConnector
{
    Task<IReadOnlyList<SmartDevice>> GetDevices();
    Task<IReadOnlyList<SmartDevice>> GetDevices(IReadOnlyList<MonitoredDevice> requestedDevices);
    ISmartHomeMonitor CreateMonitorAsync(IReadOnlyList<MonitoredDevice> devices, CancellationToken cancellationToken);
}