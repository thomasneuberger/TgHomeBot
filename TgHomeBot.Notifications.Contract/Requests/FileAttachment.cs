namespace TgHomeBot.Notifications.Contract.Requests;

/// <summary>
/// Represents a file attachment to be sent with a notification
/// </summary>
public class FileAttachment
{
    public required string FileName { get; init; }
    public required byte[] Data { get; init; }
}
