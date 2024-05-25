namespace TgHomeBot.SmartHome.Contract.Models;

public class MonitoredDevice
{
    public required string Id { get; set; }
    public required string Name { get; set; }

    public required float RunningThreshold { get; set; }

    public required float OffThreshold { get; set; }
}