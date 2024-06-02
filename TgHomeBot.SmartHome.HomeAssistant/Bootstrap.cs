using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TgHomeBot.SmartHome.Contract;
using TgHomeBot.SmartHome.Contract.Models;
using TgHomeBot.SmartHome.Contract.Requests;
using TgHomeBot.SmartHome.HomeAssistant.RequestHandlers;

namespace TgHomeBot.SmartHome.HomeAssistant;

public static class Bootstrap
{
    public static IServiceCollection AddHomeAssistant(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<HomeAssistantOptions>().Configure(options => configuration.GetSection("HomeAssistant").Bind(options));

        services.AddSingleton<ISmartHomeConnector, HomeAssistantConnector>();

        services.AddTransient<IRequestHandler<GetDevicesRequest, IReadOnlyList<SmartDevice>>, GetDevicesRequestHandler>();
        services.AddTransient<IRequestHandler<GetMonitorStateRequest, string>, GetMonitorStateRequestHandler>();

        return services;
    }
}