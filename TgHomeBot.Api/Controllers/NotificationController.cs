using MediatR;
using Microsoft.AspNetCore.Mvc;
using TgHomeBot.Api.Models;
using TgHomeBot.Notifications.Contract.Requests;
using TgHomeBot.Notifications.Telegram.Services;

namespace TgHomeBot.Api.Controllers;
[Route("api/[controller]")]
[ApiController]
public class NotificationController(IMediator mediator, IRegisteredChatService registeredChatService) : ControllerBase
{
	[HttpPost]
	public async Task<IActionResult> SendMessage(string message)
	{
		await mediator.Send(new NotifyRequest(message));

		return Ok();
	}

	[HttpPost("chat/{chatId}/flags/eurojackpot/toggle")]
	public async Task<ActionResult<ToggleFlagResponse>> ToggleEurojackpot(long chatId)
	{
		var success = await registeredChatService.ToggleEurojackpotAsync(chatId);
		
		if (!success)
		{
			return NotFound(new { message = "Chat not found" });
		}

		var chat = registeredChatService.GetRegisteredChat(chatId);
		return Ok(new ToggleFlagResponse { Enabled = chat!.EurojackpotEnabled });
	}

	[HttpPost("chat/{chatId}/flags/monthlyreport/toggle")]
	public async Task<ActionResult<ToggleFlagResponse>> ToggleMonthlyReport(long chatId)
	{
		var success = await registeredChatService.ToggleMonthlyChargingReportAsync(chatId);
		
		if (!success)
		{
			return NotFound(new { message = "Chat not found" });
		}

		var chat = registeredChatService.GetRegisteredChat(chatId);
		return Ok(new ToggleFlagResponse { Enabled = chat!.MonthlyChargingReportEnabled });
	}

	[HttpPost("chat/{chatId}/flags/devicenotifications/toggle")]
	public async Task<ActionResult<ToggleFlagResponse>> ToggleDeviceNotifications(long chatId)
	{
		var success = await registeredChatService.ToggleDeviceNotificationsAsync(chatId);
		
		if (!success)
		{
			return NotFound(new { message = "Chat not found" });
		}

		var chat = registeredChatService.GetRegisteredChat(chatId);
		return Ok(new ToggleFlagResponse { Enabled = chat!.DeviceNotificationsEnabled });
	}
}
