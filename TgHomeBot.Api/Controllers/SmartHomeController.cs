using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TgHomeBot.Api.Options;
using TgHomeBot.SmartHome.Contract.Models;
using TgHomeBot.SmartHome.Contract.Requests;

namespace TgHomeBot.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SmartHomeController(IMediator mediator) : ControllerBase
{
    [HttpGet("devices")]
    public async Task<ActionResult<SmartDevice>> GetDevices()
    {
        var devices = await mediator.Send(new GetDevicesRequest());
        return Ok(devices);
    }

    [HttpGet("devices/monitored")]
    public async Task<ActionResult<SmartDevice>> GetMonitoredDevices([FromServices]IOptions<SmartHomeOptions> options)
    {
        var devices = await mediator.Send(new GetDevicesRequest(options.Value.MonitoredDevices));
        return Ok(devices);
    }

    [HttpGet("monitor/state")]
    public ActionResult<MonitorState> GetMonitorState([FromServices] IEnumerable<IHostedService> services)
    {
        var monitoringService = services
            .OfType<MonitoringService>()
            .FirstOrDefault();

        if (monitoringService is not null)
        {
            return Ok(monitoringService.GetState());
        }
        else
        {
            return NotFound();
        }
    }
}