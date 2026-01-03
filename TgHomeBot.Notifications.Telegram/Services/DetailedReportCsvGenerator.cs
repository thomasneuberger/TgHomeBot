using System.Globalization;
using System.Text;
using TgHomeBot.Charging.Contract.Models;
using TgHomeBot.Charging.Contract.Services;

namespace TgHomeBot.Notifications.Telegram.Services;

internal class DetailedReportCsvGenerator : IDetailedReportCsvGenerator
{
    public byte[] GenerateMonthlyCsv(IReadOnlyList<ChargingSession> sessions, int year, int month)
    {
        var csv = new StringBuilder();
        
        // CSV Header
        csv.AppendLine("User,Start,End,Duration (minutes),Energy (kWh)");
        
        // Order by user name and then by car connection timestamp
        var orderedSessions = sessions
            .OrderBy(s => s.UserName)
            .ThenBy(s => s.CarConnected)
            .ToList();
        
        foreach (var session in orderedSessions)
        {
            var userName = EscapeCsvField(session.UserName);
            var startTime = session.CarConnected.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            var endTime = session.CarDisconnected?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "";
            var durationMinutes = session.ActualDurationSeconds.HasValue 
                ? (session.ActualDurationSeconds.Value / 60).ToString(CultureInfo.InvariantCulture)
                : "";
            var energy = session.KiloWattHours.ToString("F2", CultureInfo.InvariantCulture);
            
            csv.AppendLine($"{userName},{startTime},{endTime},{durationMinutes},{energy}");
        }
        
        return Encoding.UTF8.GetBytes(csv.ToString());
    }
    
    public string GetFileName(int year, int month)
    {
        // Format: YYYYMM-charging.csv (matches PDF naming schema: YYYYMM-charging.pdf)
        return $"{year:D4}{month:D2}-charging.csv";
    }
    
    private static string EscapeCsvField(string field)
    {
        // If field contains comma, quote, or newline, wrap it in quotes and escape internal quotes
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }
}
