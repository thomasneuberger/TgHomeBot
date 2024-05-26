using MediatR;

namespace TgHomeBot.Notifications.Contract.Requests;

public class NotifyRequest(string message) : IRequest
{
    public string Message => message;
}
