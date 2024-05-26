using MediatR;
using TgHomeBot.SmartHome.Contract.Models;

namespace TgHomeBot.SmartHome.Contract.Requests;

public class GetDevicesRequest(IReadOnlyList<MonitoredDevice>? devices = null) : IRequest<IReadOnlyList<SmartDevice>>
{
    public IReadOnlyList<MonitoredDevice>? Devices => devices;
}
