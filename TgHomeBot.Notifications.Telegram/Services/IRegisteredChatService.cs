using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgHomeBot.Notifications.Telegram.Models;

namespace TgHomeBot.Notifications.Telegram.Services;
internal interface IRegisteredChatService
{
    IReadOnlyList<RegisteredChat> RegisteredChats { get; }
    void Clear();
    Task<bool> RegisterChat(long userId, string username, long chatId);
    Task<bool> UnregisterChatAsync(long chatId);
    Task LoadRegisteredChats();
}
