using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TgHomeBot.SmartHome.Contract;

namespace TgHomeBot.SmartHome.HomeAssistant;

public static class Bootstrap
{
    public static IServiceCollection AddHomeAssistant(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<HomeAssistantOptions>().Configure(options => configuration.GetSection("HomeAssistant").Bind(options));
        
        services.AddScoped<ISmartHomeConnector, HomeAssistantConnector>();
        return services;
    }
}