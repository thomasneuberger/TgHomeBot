using Microsoft.AspNetCore.Mvc;
using TgHomeBot.Api.Models;
using TgHomeBot.Scheduling.Contract;
using TgHomeBot.Scheduling.Contract.Models;

namespace TgHomeBot.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SchedulerController : ControllerBase
{
    private readonly ILogger<SchedulerController> _logger;
    private readonly ISchedulerService _schedulerService;

    public SchedulerController(ILogger<SchedulerController> logger, ISchedulerService schedulerService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _schedulerService = schedulerService ?? throw new ArgumentNullException(nameof(schedulerService));
    }

    /// <summary>
    /// Lists all scheduled tasks including their configuration and next execution time
    /// </summary>
    /// <returns>Collection of scheduled task information</returns>
    [HttpGet("tasks")]
    public ActionResult<IEnumerable<ScheduledTaskInfo>> GetScheduledTasks()
    {
        var tasks = _schedulerService.GetScheduledTasks().ToList();
        return Ok(tasks);
    }

    /// <summary>
    /// Runs a specific task immediately without affecting its schedule
    /// </summary>
    /// <param name="request">Request containing the task type to run</param>
    /// <returns>Result of the task execution</returns>
    [HttpPost("tasks/run")]
    public async Task<IActionResult> RunTaskNow([FromBody] RunTaskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.TaskType))
        {
            return BadRequest("TaskType is required");
        }

        var success = await _schedulerService.RunTaskNowAsync(request.TaskType);
        
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
