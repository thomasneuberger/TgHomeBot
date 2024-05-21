using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TgHomeBot.Notifications.Contract;
using TgHomeBot.SmartHome.Contract;
using TgHomeBot.SmartHome.Contract.Models;
using TgHomeBot.SmartHome.HomeAssistant.Models;

namespace TgHomeBot.SmartHome.HomeAssistant;

internal class HomeAssistantConnector(IOptions<HomeAssistantOptions> options, HttpClient httpClient, INotificationConnector notificationConnector, ILogger<HomeAssistantMonitor> monitorLogger)
    : ISmartHomeConnector
{
    public async Task<IReadOnlyList<SmartDevice>> GetDevices()
    {
        var response = await CallApi("states", HttpMethod.Get);
        var devices = JsonSerializer.Deserialize<HomeAssistantDevice[]>(response)!;
        return devices.Select(d => Convert(d)).ToArray();
    }

    public async Task<IReadOnlyList<SmartDevice>> GetDevices(IReadOnlyList<MonitoredDevice> requestedDevices)
    {
        var devices = new List<SmartDevice>();
        foreach (var requestedDevice in requestedDevices)
        {
            var response = await CallApi($"states/{requestedDevice.Id}", HttpMethod.Get);
            var device = JsonSerializer.Deserialize<HomeAssistantDevice>(response)!;
            devices.Add(Convert(device));
        }

        return devices;
    }

    public ISmartHomeMonitor CreateMonitorAsync(IReadOnlyList<MonitoredDevice> devices, CancellationToken cancellationToken)
    {
        var monitor = new HomeAssistantMonitor(devices, options, notificationConnector, monitorLogger);
        return monitor;
    }

    private async Task<string> CallApi(string endpoint, HttpMethod method)
    {
        var url = options.Value.BaseUrl.TrimEnd('/') + "/api/" + endpoint;
        var message = new HttpRequestMessage(method, url);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.Token);

        var response = await httpClient.SendAsync(message);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    private static SmartDevice Convert(HomeAssistantDevice d)
    {
        return new SmartDevice
        {
            Id = d.EntityId,
            Name = d.Attributes.FriendlyName,
            State = string.IsNullOrWhiteSpace(d.Attributes.UnitOfMeasurement) ? d.State : $"{d.State} {d.Attributes.UnitOfMeasurement}"
        };
    }
}