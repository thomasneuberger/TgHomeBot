using TghomeBot.SmartHome.Contract.Models;

namespace TghomeBot.SmartHome.Contract;

public interface ISmartHomeConnector
{
    Task<IReadOnlyList<SmartDevice>> GetDevices();
    Task<IReadOnlyList<SmartDevice>> GetDevices(IReadOnlyList<MonitoredDevice> requestedDevices);
}