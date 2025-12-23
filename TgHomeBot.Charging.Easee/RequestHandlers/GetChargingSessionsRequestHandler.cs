using MediatR;
using TgHomeBot.Charging.Contract;
using TgHomeBot.Charging.Contract.Models;
using TgHomeBot.Charging.Contract.Requests;

namespace TgHomeBot.Charging.Easee.RequestHandlers;

internal class GetChargingSessionsRequestHandler(IChargingConnector connector) : IRequestHandler<GetChargingSessionsRequest, IReadOnlyList<ChargingSession>>
{
    public async Task<IReadOnlyList<ChargingSession>> Handle(GetChargingSessionsRequest request, CancellationToken cancellationToken)
    {
        var chargerIds = await connector.GetChargerIdsAsync(cancellationToken);

        if (chargerIds.Count == 0)
        {
            return Array.Empty<ChargingSession>();
        }

        var allSessions = new List<ChargingSession>();

        foreach (var chargerId in chargerIds)
        {
            var sessions = await connector.GetChargingSessionsAsync(chargerId, request.From, request.To, cancellationToken);
            allSessions.AddRange(sessions);
        }

        return allSessions;
    }
}
