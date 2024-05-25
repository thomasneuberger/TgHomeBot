using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgHomeBot.SmartHome.Contract.Models;
public class DeviceStateThresholds
{
    public required float RunningThreshold { get; set; }

    public required float OffThreshold { get; set; }
}
