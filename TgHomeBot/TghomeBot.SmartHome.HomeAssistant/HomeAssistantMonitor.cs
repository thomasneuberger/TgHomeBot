﻿using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TgHomeBot.SmartHome.Contract;
using TgHomeBot.SmartHome.Contract.Models;
using TgHomeBot.SmartHome.HomeAssistant.Messages;

namespace TgHomeBot.SmartHome.HomeAssistant;

public class HomeAssistantMonitor(IReadOnlyList<MonitoredDevice> devices, IOptions<HomeAssistantOptions> options, ILogger<HomeAssistantMonitor> logger)
    : ISmartHomeMonitor
{
    private readonly ClientWebSocket _webSocket = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public MonitorState State => _webSocket.State switch
    {
        WebSocketState.Open => MonitorState.Listening,
        _ => MonitorState.Idle
    };

    public async Task StartMonitoringAsync(CancellationToken cancellationToken)
    {
        var baseurl = options.Value.BaseUrl.TrimEnd('/');
        baseurl = "ws" + baseurl[baseurl.IndexOf(':')..];
        var uri = $"{baseurl}/api/websocket";

        await _webSocket.ConnectAsync(new Uri(uri), cancellationToken);

        _ = Task.Run(WaitForMessages, _cancellationTokenSource.Token);
    }

    private async Task WaitForMessages()
    {
        var message = string.Empty;
        while (_webSocket.State == WebSocketState.Open)
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
    }

    private async Task ProcessMessage(string message)
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
                ProcessEvent(message);
                break;
            default:
                logger.LogError("Unknown message of type {Type}: {Message}", genericMessage.Type, message);
                break;
        }
    }

    private void ProcessEvent(string message)
    {
        var eventMessage = JsonSerializer.Deserialize<EventMessage>(message)!;

        switch (eventMessage.EventType)
        {
            case "state_changed":
                var stateChangedEvent = JsonSerializer.Deserialize<StateChangedEventMessage>(message)!;
                if (devices.Any(d => d.Id == stateChangedEvent.Data.EntityId))
                {
                    logger.LogInformation(
                        "Device {EntityId} changed state from {OldState} to {NewState}",
                        stateChangedEvent.Data.EntityId,
                        stateChangedEvent.Data.OldState.State,
                        stateChangedEvent.Data.NewState.State);
                }
                else
                {
                    logger.LogDebug("Changed device {EntityId} not monitored", stateChangedEvent.Data.EntityId);
                }
                break;
            default:
                logger.LogError("Unknown event type {Type}: {Event}", eventMessage.EventType, message);
                break;
        }
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