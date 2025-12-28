using MediatR;

namespace TgHomeBot.Notifications.Contract.Requests;

public class NotifyRequest : IRequest
{
    public string Message { get; }
    
    public NotificationType NotificationType { get; }

    public NotifyRequest(string message, NotificationType notificationType = NotificationType.General)
    {
        Message = message;
        NotificationType = notificationType;
    }
}
