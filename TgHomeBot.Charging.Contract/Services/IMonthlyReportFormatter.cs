using TgHomeBot.Charging.Contract.Models;

namespace TgHomeBot.Charging.Contract.Services;

public interface IMonthlyReportFormatter
{
    string FormatMonthlyReport(IReadOnlyList<ChargingSession> sessions);
}
