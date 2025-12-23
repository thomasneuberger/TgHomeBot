using MediatR;
using TgHomeBot.Charging.Contract;
using TgHomeBot.Charging.Contract.Models;
using TgHomeBot.Charging.Contract.Requests;

namespace TgHomeBot.Charging.Easee.RequestHandlers;

internal class GetChargingSessionsRequestHandler(IChargingConnector connector) : IRequestHandler<GetChargingSessionsRequest, ChargingResult<IReadOnlyList<ChargingSession>>>
{
    public async Task<ChargingResult<IReadOnlyList<ChargingSession>>> Handle(GetChargingSessionsRequest request, CancellationToken cancellationToken)
    {
        var chargersResult = await connector.GetChargersAsync(cancellationToken);

        if (!chargersResult.Success)
        {
            return ChargingResult<IReadOnlyList<ChargingSession>>.Error(chargersResult.ErrorMessage!);
        }

        if (chargersResult.Data == null || chargersResult.Data.Count == 0)
        {
            return ChargingResult<IReadOnlyList<ChargingSession>>.Ok(Array.Empty<ChargingSession>());
        }

        var allSessions = new List<ChargingSession>();
        var errors = new List<string>();

        foreach (var charger in chargersResult.Data)
        {
            var sessionsResult = await connector.GetChargingSessionsAsync(charger.Id, charger.Name, request.From, request.To, cancellationToken);
            
            if (sessionsResult.Success && sessionsResult.Data != null)
            {
                allSessions.AddRange(sessionsResult.Data);
            }
            else if (!sessionsResult.Success)
            {
                errors.Add(sessionsResult.ErrorMessage ?? "Unbekannter Fehler");
            }
        }

        // If we have errors but also some sessions, return the sessions with a partial success message
        if (errors.Count > 0 && allSessions.Count > 0)
        {
            return ChargingResult<IReadOnlyList<ChargingSession>>.Ok(allSessions);
        }

        // If we have only errors and no sessions
        if (errors.Count > 0 && allSessions.Count == 0)
        {
            var errorMessage = string.Join("\n", errors);
            return ChargingResult<IReadOnlyList<ChargingSession>>.Error(errorMessage);
        }

        return ChargingResult<IReadOnlyList<ChargingSession>>.Ok(allSessions);
    }
}
