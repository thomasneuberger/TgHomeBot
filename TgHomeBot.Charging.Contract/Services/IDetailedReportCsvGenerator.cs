using TgHomeBot.Charging.Contract.Models;

namespace TgHomeBot.Charging.Contract.Services;

/// <summary>
/// Generates CSV reports for detailed charging data
/// </summary>
public interface IDetailedReportCsvGenerator
{
    /// <summary>
    /// Generates a CSV report for the given charging sessions in a specific month
    /// </summary>
    /// <param name="sessions">Charging sessions to include in the report</param>
    /// <param name="year">Year of the report</param>
    /// <param name="month">Month of the report</param>
    /// <returns>CSV file as byte array</returns>
    byte[] GenerateMonthlyCsv(IReadOnlyList<ChargingSession> sessions, int year, int month);
    
    /// <summary>
    /// Gets the file name for a monthly CSV report
    /// </summary>
    /// <param name="year">Year of the report</param>
    /// <param name="month">Month of the report</param>
    /// <returns>File name (matching PDF naming schema)</returns>
    string GetFileName(int year, int month);
}
