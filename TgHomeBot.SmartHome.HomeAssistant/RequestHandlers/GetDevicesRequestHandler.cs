using MediatR;
using TgHomeBot.SmartHome.Contract;
using TgHomeBot.SmartHome.Contract.Models;
using TgHomeBot.SmartHome.Contract.Requests;

namespace TgHomeBot.SmartHome.HomeAssistant.RequestHandlers;

internal class GetDevicesRequestHandler(ISmartHomeConnector connector) : IRequestHandler<GetDevicesRequest, IReadOnlyList<SmartDevice>>
{
    public Task<IReadOnlyList<SmartDevice>> Handle(GetDevicesRequest request, CancellationToken cancellationToken)
    {
        return request.Devices is not null
            ? connector.GetDevices(request.Devices)
            : connector.GetDevices();
    }
}
