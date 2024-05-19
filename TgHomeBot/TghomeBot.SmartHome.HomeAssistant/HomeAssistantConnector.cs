using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TghomeBot.SmartHome.Contract;
using TghomeBot.SmartHome.Contract.Models;
using TghomeBot.SmartHome.HomeAssistant.Models;

namespace TghomeBot.SmartHome.HomeAssistant;

internal class HomeAssistantConnector(IOptions<HomeAssistantOptions> options, HttpClient httpClient) : ISmartHomeConnector
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
            Name = d.Attributes.FriendlyName, 
            State = string.IsNullOrWhiteSpace(d.Attributes.UnitOfMeasurement) ? d.State : $"{d.State} {d.Attributes.UnitOfMeasurement}"
        };
    }
}