using TgHomeBot.Charging.Contract.Models;

namespace TgHomeBot.Charging.Contract.Services;

/// <summary>
/// Generates PDF reports for monthly charging data
/// </summary>
public interface IMonthlyReportPdfGenerator
{
    /// <summary>
    /// Generates a PDF report for the given charging sessions in a specific month
    /// </summary>
    /// <param name="sessions">Charging sessions to include in the report</param>
    /// <param name="year">Year of the report</param>
    /// <param name="month">Month of the report</param>
    /// <returns>PDF file as byte array</returns>
    byte[] GenerateMonthlyPdf(IReadOnlyList<ChargingSession> sessions, int year, int month);
    
    /// <summary>
    /// Gets the file name for a monthly report PDF
    /// </summary>
    /// <param name="year">Year of the report</param>
    /// <param name="month">Month of the report</param>
    /// <returns>File name (max 20 chars + .pdf extension)</returns>
    string GetFileName(int year, int month);
}
