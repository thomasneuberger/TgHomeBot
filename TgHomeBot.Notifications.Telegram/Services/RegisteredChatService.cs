﻿using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TgHomeBot.Common.Contract;
using TgHomeBot.Notifications.Telegram.Models;

namespace TgHomeBot.Notifications.Telegram.Services;

internal class RegisteredChatService : IRegisteredChatService
{
    private readonly IOptions<FileStorageOptions> _fileStorageOptions;
    private readonly ILogger<RegisteredChatService> _logger;

    private readonly List<RegisteredChat> _registeredChats = new();
    private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerOptions.Default)
    {
        WriteIndented = true
    };

    public RegisteredChatService(IOptions<FileStorageOptions> fileStorageOptions, ILogger<RegisteredChatService> logger)
    {
        _fileStorageOptions = fileStorageOptions;
        _jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        _logger = logger;
    }

    public IReadOnlyList<RegisteredChat> RegisteredChats => _registeredChats;

    public async Task LoadRegisteredChats()
    {
        var filename = Path.Combine(_fileStorageOptions.Value.Path, "RegisteredChats.json");
        if (File.Exists(filename))
        {
            var json = await File.ReadAllTextAsync(filename, Encoding.UTF8);
            var registeredChats = JsonSerializer.Deserialize<RegisteredChat[]>(json)!;
            _registeredChats.AddRange(registeredChats);
        }
    }

    public void Clear()
    {
        _registeredChats.Clear();
    }

    public async Task<bool> RegisterChat(long userId, string username, long chatId)
    {
        var existingChat = _registeredChats.FirstOrDefault(r => r.ChatId == chatId);
        if (existingChat is not null)
        {
            return false;
        }

        _registeredChats.Add(new RegisteredChat
        {
            Id = userId,
            Username = username,
            ChatId = chatId
        });

        await SaveRegisteredChats();

        _logger.LogInformation("Registered chat {ChatId} with user {User}", chatId, username);

        return true;
    }

    public async Task<bool> UnregisterChatAsync(long chatId)
    {
        var existingChat = _registeredChats.FirstOrDefault(r => r.ChatId == chatId);
        if (existingChat is null)
        {
            return false;
        }

        _registeredChats.Remove(existingChat);
        await SaveRegisteredChats();
        return true;

    }

    private async Task SaveRegisteredChats()
    {
        var json = JsonSerializer.Serialize(_registeredChats, _jsonSerializerOptions);
        await File.WriteAllTextAsync(Path.Combine(_fileStorageOptions.Value.Path, "RegisteredChats.json"), json, Encoding.UTF8);
    }

}
