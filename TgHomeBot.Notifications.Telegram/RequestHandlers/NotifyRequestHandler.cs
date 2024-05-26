using MediatR;
using TgHomeBot.Notifications.Contract;
using TgHomeBot.Notifications.Contract.Requests;

namespace TgHomeBot.Notifications.Telegram.RequestHandlers;

internal class NotifyRequestHandler(INotificationConnector connector) : IRequestHandler<NotifyRequest>
{
    public Task Handle(NotifyRequest request, CancellationToken cancellationToken)
    {
        return connector.SendAsync(request.Message);
    }
}
