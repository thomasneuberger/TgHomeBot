using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TgHomeBot.Charging.Contract;
using TgHomeBot.Charging.Contract.Models;
using TgHomeBot.Charging.Contract.Requests;
using TgHomeBot.Charging.Easee.RequestHandlers;

namespace TgHomeBot.Charging.Easee;

public static class Bootstrap
{
    public static IServiceCollection AddEasee(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EaseeOptions>().Configure(options => configuration.GetSection("Easee").Bind(options));

        services.AddSingleton<IChargingConnector, EaseeConnector>();
        services.AddSingleton<IUserAliasService, UserAliasService>();

        services.AddTransient<IRequestHandler<GetChargingSessionsRequest, ChargingResult<IReadOnlyList<ChargingSession>>>, GetChargingSessionsRequestHandler>();

        return services;
    }
}
