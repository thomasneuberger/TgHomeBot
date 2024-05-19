using TghomeBot.SmartHome.Contract.Models;

namespace TgHomeBot.Api.Options;

public class SmartHomeOptions
{
    public required MonitoredDevice[] MonitoredDevices { get; set; }
}