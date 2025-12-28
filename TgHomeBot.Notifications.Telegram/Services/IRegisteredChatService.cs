using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgHomeBot.Notifications.Telegram.Models;

namespace TgHomeBot.Notifications.Telegram.Services;
public interface IRegisteredChatService
{
    IReadOnlyList<RegisteredChat> RegisteredChats { get; }
    void Clear();
    Task<bool> RegisterChat(long userId, string username, long chatId);
    Task<bool> RegisterChat(long userId, string username, long chatId, string? chatName);
    Task<bool> UnregisterChatAsync(long chatId);
    Task LoadRegisteredChats();
    Task<bool> ToggleEurojackpotAsync(long chatId);
    Task<bool> ToggleMonthlyChargingReportAsync(long chatId);
    Task<bool> ToggleDeviceNotificationsAsync(long chatId);
    RegisteredChat? GetRegisteredChat(long chatId);
    Task UpdateChatNamesAsync();
}
