﻿using Microsoft.Extensions.Options;
using TgHomeBot.Api.Options;
using TgHomeBot.SmartHome.Contract;

namespace TgHomeBot.Api;

public class PollingService(IServiceProvider services, IOptions<SmartHomeOptions> options, ILogger<PollingService> logger) : IHostedService
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private readonly IDictionary<string, string> _lastDeviceStates = new Dictionary<string, string>(options.Value.MonitoredDevices.Length);
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(PollDeviceStates, _cancellationTokenSource.Token);
        
        return Task.CompletedTask;
    }

    private async Task PollDeviceStates()
    {
        while (!_cancellationTokenSource.IsCancellationRequested)
        {
            using var scope = services.CreateScope();
            var smartHomeConnector = scope.ServiceProvider.GetRequiredService<ISmartHomeConnector>();
            var deviceStates = await smartHomeConnector.GetDevices(options.Value.MonitoredDevices);
            foreach (var deviceState in deviceStates)
            {
                if (_lastDeviceStates.TryGetValue(deviceState.Id, out var lastState) && lastState != deviceState.State)
                {
                    logger.LogInformation("State of device {EntityId} changed from {OldState} to {NewState}", deviceState.Id, lastState, deviceState.State);
                }

                _lastDeviceStates[deviceState.Id] = deviceState.State;
            }

            await Task.Delay(60_000);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _cancellationTokenSource.CancelAsync();
    }
}