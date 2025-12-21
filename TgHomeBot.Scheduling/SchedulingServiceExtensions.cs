using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TgHomeBot.Scheduling;

/// <summary>
/// Extension methods for registering scheduling services
/// </summary>
public static class SchedulingServiceExtensions
{
    /// <summary>
    /// Adds the scheduling service to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddScheduling(this IServiceCollection services)
    {
        services.AddSingleton<IHostedService, SchedulerService>();

        return services;
    }
}
