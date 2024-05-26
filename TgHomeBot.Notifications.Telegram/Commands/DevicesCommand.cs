using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgHomeBot.Common.Contract;
using TgHomeBot.SmartHome.Contract.Requests;

namespace TgHomeBot.Notifications.Telegram.Commands;

internal class DevicesCommand(IOptions<SmartHomeOptions> options, IServiceProvider serviceProvider) : ICommand
{
    public string Name => "/devices";
    public string Description => "Die überwachten Geräte auflisten";
    public async Task ProcessMessage(Message message, ITelegramBotClient client, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var devices = await mediator.Send(new GetDevicesRequest(options.Value.MonitoredDevices), cancellationToken);

        var deviceStates = string.Join('\n', devices.Select(d => $"{d.Name}: {d.State}"));

        await client.SendTextMessageAsync(new ChatId(message.Chat.Id), deviceStates, cancellationToken: cancellationToken);
    }
}
