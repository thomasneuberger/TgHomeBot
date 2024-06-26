﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;
using TgHomeBot.SmartHome.Contract;
using TgHomeBot.SmartHome.Contract.Models;
using TgHomeBot.SmartHome.HomeAssistant.Models;

namespace TgHomeBot.SmartHome.HomeAssistant;

internal class HomeAssistantConnector(IOptions<HomeAssistantOptions> options, HttpClient httpClient, IServiceProvider serviceProvider, ILogger<HomeAssistantMonitor> monitorLogger)
    : ISmartHomeConnector
{
    private ISmartHomeMonitor? _smartHomeMonitor;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public async Task<IReadOnlyList<SmartDevice>> GetDevices()
    {
        var response = await CallApi("states", HttpMethod.Get);
        var devices = JsonSerializer.Deserialize<HomeAssistantDevice[]>(response)!;
        return devices.Select(d => ConvertDevice(d)).ToArray();
    }

    public async Task<IReadOnlyList<SmartDevice>> GetDevices(IReadOnlyList<MonitoredDevice> requestedDevices)
    {
        var devices = new List<SmartDevice>();
        foreach (var requestedDevice in requestedDevices)
        {
            var response = await CallApi($"states/{requestedDevice.Id}", HttpMethod.Get);
            var device = JsonSerializer.Deserialize<HomeAssistantDevice>(response)!;
            devices.Add(ConvertDevice(device, requestedDevice.Name));
        }

        return devices;
    }

    public async Task<ISmartHomeMonitor> CreateMonitorAsync(IReadOnlyList<MonitoredDevice> devices,
        CancellationToken cancellationToken)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken);
            if (_smartHomeMonitor is null)
            {
                _smartHomeMonitor = new HomeAssistantMonitor(devices, options, serviceProvider, monitorLogger);
            }

            return _smartHomeMonitor;
        }
        finally
        {
            _semaphore.Release();
        }
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

    private static SmartDevice ConvertDevice(HomeAssistantDevice d, string? name = null)
    {
        return new SmartDevice
        {
            Id = d.EntityId,
            Name = name ?? d.Attributes.FriendlyName,
            State = string.IsNullOrWhiteSpace(d.Attributes.UnitOfMeasurement) ? d.State : $"{d.State} {d.Attributes.UnitOfMeasurement}"
        };
    }
}