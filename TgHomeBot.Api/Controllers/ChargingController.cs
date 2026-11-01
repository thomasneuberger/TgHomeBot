using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TgHomeBot.Charging.Contract.Requests;
using TgHomeBot.Charging.Contract.Services;
using TgHomeBot.Common.Contract;

namespace TgHomeBot.Api.Controllers;

/// <summary>
/// Controller for charging-related operations and report generation
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class ChargingController(
    ILogger<ChargingController> logger,
    IMediator mediator,
    IMonthlyReportFormatter monthlyReportFormatter,
    IMonthlyReportPdfGenerator monthlyReportPdfGenerator,
    IDetailedReportCsvGenerator detailedReportCsvGenerator,
    IOptions<FileStorageOptions> fileStorageOptions) : ControllerBase
{
    private readonly ILogger<ChargingController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly IMonthlyReportFormatter _monthlyReportFormatter = monthlyReportFormatter ?? throw new ArgumentNullException(nameof(monthlyReportFormatter));
    private readonly IMonthlyReportPdfGenerator _monthlyReportPdfGenerator = monthlyReportPdfGenerator ?? throw new ArgumentNullException(nameof(monthlyReportPdfGenerator));
    private readonly IDetailedReportCsvGenerator _detailedReportCsvGenerator = detailedReportCsvGenerator ?? throw new ArgumentNullException(nameof(detailedReportCsvGenerator));
    private readonly FileStorageOptions _fileStorageOptions = fileStorageOptions?.Value ?? throw new ArgumentNullException(nameof(fileStorageOptions));

    /// <summary>
    /// Generates a formatted charging report for a specific month and year
    /// </summary>
    /// <param name="year">The year of the report</param>
    /// <param name="month">The month of the report (1-12)</param>
    /// <returns>Formatted charging report as text</returns>
    [HttpGet("reports/{year:int}/{month:int}")]
    public async Task<IActionResult> GetMonthlyReport(int year, int month)
    {
        if (!ValidateYearMonth(year, month))
        {
            return BadRequest(new { message = "Invalid year or month. Month must be between 1 and 12." });
        }

        try
        {
            var from = new DateTime(year, month, 1);
            var to = from.AddMonths(1).AddDays(-1);

            _logger.LogInformation("Retrieving charging sessions for {Year}-{Month}", year, month);

            var result = await _mediator.Send(new GetChargingSessionsRequest(from, to));

            if (!result.Success)
            {
                _logger.LogWarning("Failed to retrieve charging sessions: {ErrorMessage}", result.ErrorMessage);
                return BadRequest(new { message = result.ErrorMessage ?? "Failed to retrieve charging sessions" });
            }

            if (result.Data == null || result.Data.Count == 0)
            {
                _logger.LogInformation("No charging sessions found for {Year}-{Month}", year, month);
                return Ok(new { report = $"No charging sessions found for {year:D4}-{month:D2}." });
            }

            var formattedReport = _monthlyReportFormatter.FormatMonthlyReport(result.Data);
            _logger.LogInformation("Successfully generated charging report for {Year}-{Month}", year, month);

            return Ok(new { report = formattedReport });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating charging report for {Year}-{Month}", year, month);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "An error occurred while generating the report. Check logs for details." });
        }
    }

    /// <summary>
    /// Generates a PDF charging report for a specific month and year
    /// </summary>
    /// <param name="year">The year of the report</param>
    /// <param name="month">The month of the report (1-12)</param>
    /// <returns>PDF file download</returns>
    [HttpPost("reports/{year:int}/{month:int}/pdf")]
    public async Task<IActionResult> GetMonthlyReportPdf(int year, int month)
    {
        if (!ValidateYearMonth(year, month))
        {
            return BadRequest(new { message = "Invalid year or month. Month must be between 1 and 12." });
        }

        try
        {
            var from = new DateTime(year, month, 1);
            var to = from.AddMonths(1).AddDays(-1);

            _logger.LogInformation("Retrieving charging sessions for report generation {Year}-{Month}", year, month);

            var result = await _mediator.Send(new GetChargingSessionsRequest(from, to));

            if (!result.Success)
            {
                _logger.LogWarning("Failed to retrieve charging sessions for report generation: {ErrorMessage}", result.ErrorMessage);
                return BadRequest(new { message = result.ErrorMessage ?? "Failed to retrieve charging sessions" });
            }

            if (result.Data == null || result.Data.Count == 0)
            {
                _logger.LogInformation("No charging sessions found for report generation {Year}-{Month}", year, month);
                return Ok(new { message = $"No charging sessions found for {year:D4}-{month:D2}." });
            }

            _logger.LogInformation("Generating PDF and CSV reports for {Year}-{Month}", year, month);

            var pdfBytes = _monthlyReportPdfGenerator.GenerateMonthlyPdf(result.Data, year, month);
            var pdfFileName = _monthlyReportPdfGenerator.GetFileName(year, month);

            var csvBytes = _detailedReportCsvGenerator.GenerateMonthlyCsv(result.Data, year, month);
            var csvFileName = _detailedReportCsvGenerator.GetFileName(year, month);

            await SaveReportToStorage(pdfFileName, pdfBytes);
            await SaveReportToStorage(csvFileName, csvBytes);

            return File(pdfBytes, "application/pdf", pdfFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating reports for {Year}-{Month}", year, month);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while generating the reports. Check logs for details." });
        }
    }

    /// <summary>
    /// Validates the year and month parameters
    /// </summary>
    /// <param name="year">The year to validate</param>
    /// <param name="month">The month to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    private static bool ValidateYearMonth(int year, int month)
    {
        return year >= 1900 && year <= DateTime.UtcNow.Year && month >= 1 && month <= 12;
    }

    private async Task SaveReportToStorage(string fileName, byte[] fileData)
    {
        var directory = Path.Combine(_fileStorageOptions.Path, "monthly-reports");
        Directory.CreateDirectory(directory);

        var filePath = Path.Combine(directory, fileName);
        await System.IO.File.WriteAllBytesAsync(filePath, fileData);

        _logger.LogInformation("Saved report file to {FilePath}", filePath);
    }
}
