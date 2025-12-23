using MediatR;
using TgHomeBot.Charging.Contract.Models;

namespace TgHomeBot.Charging.Contract.Requests;

/// <summary>
/// Request to get charging sessions for all chargers within a date range
/// </summary>
public class GetChargingSessionsRequest(DateTime from, DateTime to) : IRequest<IReadOnlyList<ChargingSession>>
{
    public DateTime From => from;
    public DateTime To => to;
}
