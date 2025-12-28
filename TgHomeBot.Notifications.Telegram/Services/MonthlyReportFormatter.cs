using System.Globalization;
using TgHomeBot.Charging.Contract.Models;
using TgHomeBot.Charging.Contract.Services;

namespace TgHomeBot.Notifications.Telegram.Services;

internal class MonthlyReportFormatter : IMonthlyReportFormatter
{
    public string FormatMonthlyReport(IReadOnlyList<ChargingSession> sessions)
    {
        if (sessions.Count == 0)
        {
            return "Keine Ladevorgänge in den letzten zwei Monaten gefunden.";
        }

        // Group by user and month, then sum the energy
        var monthlyReport = sessions
            .GroupBy(s => new { s.UserName, Year = s.CarConnected.Year, Month = s.CarConnected.Month })
            .Select(g => new
            {
                g.Key.UserName,
                g.Key.Year,
                g.Key.Month,
                TotalKwh = g.Sum(s => s.KiloWattHours)
            })
            .OrderBy(x => x.UserName)
            .ThenBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToList();

        var reportLines = new List<string> { "📊 Monatlicher Ladebericht (letzte 2 Monate):" };

        foreach (var entry in monthlyReport)
        {
            var monthName = new DateTime(entry.Year, entry.Month, 1).ToString("MMMM yyyy", CultureInfo.GetCultureInfo("de-DE"));
            reportLines.Add($"👤 {entry.UserName} - {monthName}: {entry.TotalKwh:F2} kWh");
        }

        return string.Join('\n', reportLines);
    }
}
