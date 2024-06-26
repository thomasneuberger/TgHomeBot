﻿using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TgHomeBot.Notifications.Contract.Requests;
using TgHomeBot.SmartHome.Contract;
using TgHomeBot.SmartHome.Contract.Models;
using TgHomeBot.SmartHome.HomeAssistant.Messages;
using TgHomeBot.SmartHome.HomeAssistant.Models;

namespace TgHomeBot.SmartHome.HomeAssistant;

public class HomeAssistantMonitor(
    IReadOnlyList<MonitoredDevice> devices,
    IOptions<HomeAssistantOptions> options,
    IServiceProvider serviceProvider,
    ILogger<HomeAssistantMonitor> logger)
    : ISmartHomeMonitor
{
    private readonly ClientWebSocket _webSocket = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private bool _reconnect;

    public MonitorState State => _webSocket.State switch
    {
        WebSocketState.Open => MonitorState.Listening,
        _ => MonitorState.Idle
    };

    public async Task StartMonitoringAsync(CancellationToken cancellationToken)
    {
        if (_webSocket.State == WebSocketState.Open)
        {
            return;
        }

        var baseurl = options.Value.BaseUrl.TrimEnd('/');
        baseurl = "ws" + baseurl[baseurl.IndexOf(':')..];
        var uri = $"{baseurl}/api/websocket";

        await _webSocket.ConnectAsync(new Uri(uri), cancellationToken);

        _reconnect = true;

        _ = Task.Run(WaitForMessages, _cancellationTokenSource.Token);
    }

    private async Task WaitForMessages()
    {
        var message = string.Empty;
        while (_webSocket.State == WebSocketState.Open)
        {
            try
            {
                var buffer = new byte[1024];
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);
                message += Encoding.UTF8.GetString(buffer, 0, result.Count);
                if (result.EndOfMessage)
                {
                    await ProcessMessage(message);
                    message = string.Empty;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error receiving message from Home Assistant web socket: {Exception}", ex.Message);
            }
        }
        logger.LogWarning("Home Assistant Web socket has been closed.");
        if (_reconnect)
        {
            while (_webSocket.State == WebSocketState.Closed)
            {
                logger.LogInformation("Reconnect...");
                await StartMonitoringAsync(_cancellationTokenSource.Token);
                if (_webSocket.State != WebSocketState.Open)
                {
                    await Task.Delay(5000);
                }
            }
        }
    }

    private async Task ProcessMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        try
        {
            var genericMessage = JsonSerializer.Deserialize<GenericMessage>(message)!;

            switch (genericMessage.Type)
            {
                case "auth_required":
                    var authRequiredMessage = JsonSerializer.Deserialize<VersionMessage>(message)!;
                    logger.LogInformation("Authentication requested: {Version}", authRequiredMessage.HomeAssistantVersion);
                    await SendMessageAsync(new AuthMessage(options.Value.Token));
                    break;
                case "auth_ok":
                    var authOkMessage = JsonSerializer.Deserialize<VersionMessage>(message)!;
                    logger.LogInformation("Authentication successful: {Version}", authOkMessage.HomeAssistantVersion);
                    var subscribeMessage = new SubscribeMessage
                    {
                        Id = SubscribeMessage.StateChangedId,
                        EventType = SubscribeMessage.StateChangedEventType
                    };
                    logger.LogInformation("Subscribe to state changed events");
                    await SendMessageAsync(subscribeMessage);
                    break;
                case "auth_invalid":
                    var authInvalidMessage = JsonSerializer.Deserialize<AuthInvalidMessage>(message)!;
                    logger.LogError("Authentication invalid: {Message}", authInvalidMessage.Message);
                    break;
                case "result":
                    var resultMessage = JsonSerializer.Deserialize<ResultMessage>(message)!;
                    if (resultMessage.Success)
                    {
                        logger.LogInformation("Request {Id} successful: {Message}", resultMessage.Id, message);
                    }
                    else
                    {
                        logger.LogInformation("Request {Id} not successful: {Message}", resultMessage.Id, message);
                    }
                    break;
                case "event":
                    await ProcessEvent(message);
                    break;
                default:
                    logger.LogError("Unknown message of type {Type}: {Message}", genericMessage.Type, message);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing message: {Exception}", ex);
        }
    }

    private async Task ProcessEvent(string message)
    {
        var eventMessage = JsonSerializer.Deserialize<EventMessage<object>>(message)!;

        switch (eventMessage.Event.EventType)
        {
            case "state_changed":
                var stateChangedEvent = JsonSerializer.Deserialize<StateChangedEventMessage>(message)!;
                var monitoredDevice = devices.FirstOrDefault(d => d.Id == stateChangedEvent.Event.Data.EntityId);
                if (monitoredDevice is not null)
                {
                    if (monitoredDevice.StateThresholds is not null)
                    {
                        var oldState = GetState(monitoredDevice.StateThresholds, stateChangedEvent.Event.Data.OldState.State);
                        var newState = GetState(monitoredDevice.StateThresholds, stateChangedEvent.Event.Data.NewState.State);
                        logger.LogInformation(
                            "Device {Device} changed state from {OldStateValue} ({OldState}) to {NewStateValue} ({NewState})",
                            monitoredDevice.Name,
                            stateChangedEvent.Event.Data.OldState.State,
                            oldState,
                            stateChangedEvent.Event.Data.NewState.State,
                            newState);

                        if (oldState == DeviceState.Running && (newState != DeviceState.Running))
                        {
                            using var scope = serviceProvider.CreateScope();
                            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                            await mediator.Send(new NotifyRequest($"{monitoredDevice.Name} ist fertig."));
                        }
                    }
                    else
                    {
                        var oldState = stateChangedEvent.Event.Data.OldState.State;
                        var newState = stateChangedEvent.Event.Data.NewState.State;
                        logger.LogInformation(
                            "Device {Device} changed state from {OldStateValue} to {NewStateValue}",
                            monitoredDevice.Name,
                            oldState,
                            newState);
                    }
                }
                else
                {
                    logger.LogDebug("Changed device {EntityId} not monitored", stateChangedEvent.Event.Data.EntityId);
                }
                break;
            default:
                logger.LogError("Unknown event type {Type}: {Event}", eventMessage.Event.EventType, message);
                break;
        }
    }

    private static DeviceState GetState(DeviceStateThresholds deviceThresholds, string state)
    {
        if (!float.TryParse(state, out var value))
        {
            return DeviceState.Unknown;
        }

        if (value > deviceThresholds.RunningThreshold)
        {
            return DeviceState.Running;
        }

        if (value < deviceThresholds.OffThreshold)
        {
            return DeviceState.Off;
        }

        return DeviceState.Waiting;

    }

    private async Task SendMessageAsync<TMessage>(TMessage message)
        where TMessage: IMessage
    {
        var serialized = JsonSerializer.Serialize(message);
        var data = Encoding.UTF8.GetBytes(serialized);
        await _webSocket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage, _cancellationTokenSource.Token);
    }

    public async Task StopMonitoring(CancellationToken cancellationToken)
    {
        _reconnect = false;
        await _cancellationTokenSource.CancelAsync();
        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", cancellationToken);
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _webSocket.Dispose();
        _cancellationTokenSource.Dispose();
    }
}