using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TgHomeBot.Notifications.Contract;
using TgHomeBot.Notifications.Contract.Requests;
using TgHomeBot.Notifications.Telegram.RequestHandlers;

namespace TgHomeBot.Notifications.Telegram;
public static class Bootstrap
{
	public static IServiceCollection AddTelegram(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddOptions<TelegramOptions>().Configure(options => configuration.GetSection("Telegram").Bind(options));

		services.AddSingleton<INotificationConnector, TelegramConnector>();

        services.AddTransient<IRequestHandler<NotifyRequest>, NotifyRequestHandler>();

		return services;
	}
}
