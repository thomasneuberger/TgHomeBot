using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgHomeBot.Notifications.Contract;

public interface INotificationConnector
{
	Task SendAsync(string message);
	Task Connect();
	Task DisconnectAsync();
}
