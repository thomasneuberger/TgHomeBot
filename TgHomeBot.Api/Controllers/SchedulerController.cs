using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using TgHomeBot.Api.Models;
using TgHomeBot.Scheduling;

namespace TgHomeBot.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SchedulerController : ControllerBase
{
    private readonly ILogger<SchedulerController> _logger;

    public SchedulerController(ILogger<SchedulerController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Lists all scheduled tasks including their configuration and next execution time
    /// </summary>
    /// <returns>Collection of scheduled task information</returns>
    [HttpGet("tasks")]
    public ActionResult<IEnumerable<ScheduledTaskInfo>> GetScheduledTasks([FromServices] IEnumerable<IHostedService> services)
    {
        var schedulerService = services
            .OfType<SchedulerService>()
            .FirstOrDefault();

        if (schedulerService is null)
        {
            _logger.LogWarning("SchedulerService not found");
            return NotFound("Scheduler service is not available");
        }

        var tasks = schedulerService.GetScheduledTasks()
            .Select(t => new ScheduledTaskInfo
            {
                TaskType = t.TaskType,
                TaskName = t.TaskName,
                CronExpression = t.CronExpression,
                Enabled = t.Enabled,
                NextExecutionTime = t.NextExecutionTime
            })
            .ToList();

        return Ok(tasks);
    }

    /// <summary>
    /// Runs a specific task immediately without affecting its schedule
    /// </summary>
    /// <param name="request">Request containing the task type to run</param>
    /// <returns>Result of the task execution</returns>
    [HttpPost("tasks/run")]
    public async Task<IActionResult> RunTaskNow([FromBody] RunTaskRequest request, [FromServices] IEnumerable<IHostedService> services)
    {
        if (string.IsNullOrWhiteSpace(request?.TaskType))
        {
            return BadRequest("TaskType is required");
        }

        var schedulerService = services
            .OfType<SchedulerService>()
            .FirstOrDefault();

        if (schedulerService is null)
        {
            _logger.LogWarning("SchedulerService not found");
            return NotFound("Scheduler service is not available");
        }

        var success = await schedulerService.RunTaskNowAsync(request.TaskType);
        
        if (success)
        {
            return Ok(new { message = $"Task {request.TaskType} executed successfully" });
        }
        else
        {
            return BadRequest(new { message = $"Failed to execute task {request.TaskType}. Check logs for details." });
        }
    }
}
