namespace TgHomeBot.Common.Contract;

/// <summary>
/// Options for application-wide configuration
/// </summary>
public class ApplicationOptions
{
    /// <summary>
    /// The base URL where the application is hosted (e.g., "http://localhost:5000" or "https://myapp.example.com")
    /// </summary>
    public required string BaseUrl { get; set; }
}
