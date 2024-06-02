using MediatR;
using Microsoft.Extensions.Options;
using TgHomeBot.Common.Contract;
using TgHomeBot.SmartHome.Contract;
using TgHomeBot.SmartHome.Contract.Requests;

namespace TgHomeBot.SmartHome.HomeAssistant.RequestHandlers;

internal class GetMonitorStateRequestHandler(ISmartHomeConnector smartHomeConnector, IOptions<SmartHomeOptions> options) : IRequestHandler<GetMonitorStateRequest, string>
{
    public async Task<string> Handle(GetMonitorStateRequest request, CancellationToken cancellationToken)
    {
        var monitor = await smartHomeConnector.CreateMonitorAsync(options.Value.MonitoredDevices, cancellationToken);
        return monitor.State.ToString();
    }
}
