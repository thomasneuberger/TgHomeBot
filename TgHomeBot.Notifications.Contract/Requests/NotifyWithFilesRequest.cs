using MediatR;

namespace TgHomeBot.Notifications.Contract.Requests;

/// <summary>
/// Represents a file attachment to be sent with a notification
/// </summary>
public class FileAttachment
{
    public required string FileName { get; init; }
    public required byte[] Data { get; init; }
}

/// <summary>
/// Request to send a notification with file attachments
/// </summary>
public class NotifyWithFilesRequest : IRequest
{
    public string Message { get; }
    
    public NotificationType NotificationType { get; }
    
    public IReadOnlyList<FileAttachment> Files { get; }

    public NotifyWithFilesRequest(string message, IReadOnlyList<FileAttachment> files, NotificationType notificationType = NotificationType.General)
    {
        Message = message;
        Files = files;
        NotificationType = notificationType;
    }
}
