namespace TgHomeBot.SmartHome.Contract.Models;

public class DeviceStateThresholds
{
    public float? RunningThreshold { get; set; }

    public float? OffThreshold { get; set; }

    public float? AboveThreshold { get; set; }
}
