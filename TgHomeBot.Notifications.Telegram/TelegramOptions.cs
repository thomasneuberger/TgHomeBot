using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgHomeBot.Notifications.Telegram;
internal class TelegramOptions
{
	public required string Token { get; set; }

	public required string[] AllowedUserNames { get; set; }
}
