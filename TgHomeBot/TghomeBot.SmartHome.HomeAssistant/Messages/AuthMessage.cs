using System.Text.Json.Serialization;

namespace TgHomeBot.SmartHome.HomeAssistant.Messages;

public class AuthMessage(string accessToken) : IMessage
{
    [JsonPropertyName("type")]
    public string Type => "auth";
    
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = accessToken;
}