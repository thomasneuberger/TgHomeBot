using Microsoft.AspNetCore.Mvc;
using TgHomeBot.Notifications.Contract;

namespace TgHomeBot.Api.Controllers;
[Route("api/[controller]")]
[ApiController]
public class NotificationController(INotificationConnector connector) : ControllerBase
{
	[HttpPost]
	public async Task<IActionResult> SendMessage(string message)
	{
		await connector.SendAsync(message);

		return Ok();
	}
}
