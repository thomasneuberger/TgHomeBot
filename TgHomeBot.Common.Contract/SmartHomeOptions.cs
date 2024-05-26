using TgHomeBot.SmartHome.Contract.Models;

namespace TgHomeBot.Common.Contract;

public class SmartHomeOptions
{
    public required MonitoredDevice[] MonitoredDevices { get; set; }
}