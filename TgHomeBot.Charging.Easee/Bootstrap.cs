using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TgHomeBot.Charging.Contract;

namespace TgHomeBot.Charging.Easee;

public static class Bootstrap
{
    public static IServiceCollection AddEasee(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EaseeOptions>().Configure(options => configuration.GetSection("Easee").Bind(options));

        services.AddSingleton<IChargingConnector, EaseeConnector>();

        return services;
    }
}
