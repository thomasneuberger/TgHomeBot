﻿namespace TgHomeBot.SmartHome.Contract.Models;

public class MonitoredDevice
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public DeviceStateThresholds? StateThresholds { get; set; }
}