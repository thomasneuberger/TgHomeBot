using MediatR;
using Microsoft.AspNetCore.Mvc;
using TgHomeBot.Notifications.Contract.Requests;

namespace TgHomeBot.Api.Controllers;
[Route("api/[controller]")]
[ApiController]
public class NotificationController(IMediator mediator) : ControllerBase
{
	[HttpPost]
	public async Task<IActionResult> SendMessage(string message)
	{
		await mediator.Send(new NotifyRequest(message));

		return Ok();
	}
}
