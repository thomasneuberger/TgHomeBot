using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TgHomeBot.Notifications.Contract;

namespace TgHomeBot.Scheduling.Tasks;

/// <summary>
/// Scheduled task to report the current Eurojackpot lottery jackpot to Telegram
/// Runs every Tuesday and Friday at 10pm (22:00)
/// </summary>
public class JackpotReportTask : IScheduledTask
{
    private const string EurojackpotApiUrl = "https://media.lottoland.com/api/drawings/euroJackpot";
    
    private readonly ILogger<JackpotReportTask> _logger;
    private readonly INotificationConnector _notificationConnector;
    private readonly IHttpClientFactory _httpClientFactory;

    public string TaskName => "JackpotReportTask";

    public JackpotReportTask(
        ILogger<JackpotReportTask> logger,
        INotificationConnector notificationConnector,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notificationConnector = notificationConnector ?? throw new ArgumentNullException(nameof(notificationConnector));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Eurojackpot jackpot report task");

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(EurojackpotApiUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch Eurojackpot data. Status code: {StatusCode}", response.StatusCode);
                return;
            }

            EurojackpotApiResponse? apiResponse;
            try
            {
                apiResponse = await response.Content.ReadFromJsonAsync<EurojackpotApiResponse>(cancellationToken);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize Eurojackpot API response");
                return;
            }

            if (apiResponse?.Last == null)
            {
                _logger.LogWarning("No Eurojackpot results found in API response");
                return;
            }

            var message = FormatJackpotMessage(apiResponse.Last, apiResponse.Next);

            await _notificationConnector.SendAsync(message, NotificationType.Eurojackpot);
            _logger.LogInformation("Successfully sent Eurojackpot jackpot report");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Eurojackpot jackpot report task");
        }
    }

    private static string FormatJackpotMessage(EurojackpotDraw lastDraw, EurojackpotDraw? nextDraw)
    {
        var message = $"🎰 <b>Eurojackpot Ziehung</b>\n\n";
        
        // Last draw information
        message += $"📅 <b>Letzte Ziehung:</b> {FormatDate(lastDraw.Date)}\n";
        message += $"🔢 Gewinnzahlen: {string.Join(", ", lastDraw.Numbers)}\n";
        message += $"⭐ Eurozahlen: {string.Join(", ", lastDraw.EuroNumbers)}\n";
        
        if (lastDraw.Jackpot > 0)
        {
            message += $"💰 Jackpot: {FormatJackpotAmount(lastDraw.Jackpot)}\n";
        }
        
        // Next draw information if available
        if (nextDraw != null)
        {
            message += $"\n📅 <b>Nächste Ziehung:</b> {FormatDate(nextDraw.Date)}\n";
            if (nextDraw.Jackpot > 0)
            {
                message += $"💰 Erwarteter Jackpot: {FormatJackpotAmount(nextDraw.Jackpot)}\n";
            }
        }
        
        message += "\nViel Glück! 🍀";
        
        return message;
    }

    private static string FormatDate(DrawDate? date)
    {
        if (date == null || string.IsNullOrEmpty(date.Full))
        {
            return "Unbekannt";
        }

        // Try to parse and format the date nicely
        if (DateTime.TryParse(date.Full, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            return parsedDate.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("de-DE"));
        }

        return date.Full;
    }

    private static string FormatJackpotAmount(long jackpot)
    {
        return jackpot.ToString("N0", CultureInfo.GetCultureInfo("de-DE")) + " €";
    }

    private class EurojackpotApiResponse
    {
        [JsonPropertyName("last")]
        public EurojackpotDraw? Last { get; set; }

        [JsonPropertyName("next")]
        public EurojackpotDraw? Next { get; set; }
    }

    private class EurojackpotDraw
    {
        [JsonPropertyName("date")]
        public DrawDate? Date { get; set; }

        [JsonPropertyName("numbers")]
        public List<int> Numbers { get; set; } = new();

        [JsonPropertyName("euroNumbers")]
        public List<int> EuroNumbers { get; set; } = new();

        [JsonPropertyName("jackpot")]
        public long Jackpot { get; set; }
    }

    private class DrawDate
    {
        [JsonPropertyName("full")]
        public string Full { get; set; } = string.Empty;
    }
}
