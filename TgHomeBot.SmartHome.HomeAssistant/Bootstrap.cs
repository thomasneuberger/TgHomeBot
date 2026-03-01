using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
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

        services.AddHttpClient(HomeAssistantConnector.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(sp =>
            {
                var options = sp.GetRequiredService<IOptions<HomeAssistantOptions>>().Value;
                var handler = new HttpClientHandler();
                if (!string.IsNullOrEmpty(options.CertificateAuthorityPath))
                {
                    var certificate = X509CertificateLoader.LoadCertificateFromFile(options.CertificateAuthorityPath);
                    handler.ServerCertificateCustomValidationCallback = (_, cert, chain, errors) =>
                    {
                        if (errors == SslPolicyErrors.None) return true;
                        if (chain is null || cert is null) return false;
                        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                        chain.ChainPolicy.CustomTrustStore.Add(certificate);
                        return chain.Build(cert);
                    };
                }
                return handler;
            });

        services.AddSingleton<ISmartHomeConnector, HomeAssistantConnector>();

        services.AddTransient<IRequestHandler<GetDevicesRequest, IReadOnlyList<SmartDevice>>, GetDevicesRequestHandler>();
        services.AddTransient<IRequestHandler<GetMonitorStateRequest, string>, GetMonitorStateRequestHandler>();

        return services;
    }
}