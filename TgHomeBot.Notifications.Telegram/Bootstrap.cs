using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TgHomeBot.Charging.Contract.Services;
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
        services.AddSingleton<IMonthlyReportFormatter, MonthlyReportFormatter>();
        services.AddSingleton<IMonthlyReportPdfGenerator, MonthlyReportPdfGenerator>();

        services.AddSingleton<ICommand, StartCommand>();
        services.AddSingleton<ICommand, CheckCommand>();
        services.AddSingleton<ICommand, EndCommand>();
        services.AddSingleton<ICommand, MonitoredDevicesCommand>();
        services.AddSingleton<ICommand, DevicesCommand>();
        services.AddSingleton<ICommand, ScheduledTasksCommand>();
        services.AddSingleton<ICommand, RunTaskCommand>();
        services.AddSingleton<ICommand, HelpCommand>();
        services.AddSingleton<ICommand, LogCommand>();
        services.AddSingleton<ICommand, LogFileCommand>();
        services.AddSingleton<ICommand, MonthlyReportCommand>();
        services.AddSingleton<ICommand, DetailedReportCommand>();
        services.AddSingleton<ICommand, ToggleEurojackpotCommand>();
        services.AddSingleton<ICommand, ToggleMonthlyReportCommand>();
        services.AddSingleton<ICommand, ToggleDeviceNotificationsCommand>();
        services.AddSingleton<ICommand, FlagsCommand>();

		services.AddSingleton<INotificationConnector, TelegramConnector>();

        services.AddTransient<IRequestHandler<NotifyRequest>, NotifyRequestHandler>();

		return services;
	}
}
