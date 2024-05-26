using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TgHomeBot.Notifications.Contract;
using TgHomeBot.Notifications.Contract.Requests;
using TgHomeBot.Notifications.Telegram.Commands;
using TgHomeBot.Notifications.Telegram.RequestHandlers;
using TgHomeBot.Notifications.Telegram.Services;

namespace TgHomeBot.Notifications.Telegram;
public static class Bootstrap
{
	public static IServiceCollection AddTelegram(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddOptions<TelegramOptions>().Configure(options => configuration.GetSection("Telegram").Bind(options));

        services.AddSingleton<IRegisteredChatService, RegisteredChatService>();

        services.AddSingleton<ICommand, StartCommand>();
        services.AddSingleton<ICommand, CheckCommand>();
        services.AddSingleton<ICommand, EndCommand>();
        services.AddSingleton<ICommand, MonitoredDevicesCommand>();
        services.AddSingleton<ICommand, DevicesCommand>();
        services.AddSingleton<ICommand, HelpCommand>();

		services.AddSingleton<INotificationConnector, TelegramConnector>();

        services.AddTransient<IRequestHandler<NotifyRequest>, NotifyRequestHandler>();

		return services;
	}
}
