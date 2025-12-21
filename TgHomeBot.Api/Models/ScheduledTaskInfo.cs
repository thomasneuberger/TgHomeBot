namespace TgHomeBot.Api.Models;

/// <summary>
/// Information about a scheduled task
/// </summary>
public class ScheduledTaskInfo
{
    /// <summary>
    /// The type name of the task
    /// </summary>
    public string TaskType { get; set; } = string.Empty;

    /// <summary>
    /// The name of the task
    /// </summary>
    public string TaskName { get; set; } = string.Empty;

    /// <summary>
    /// The cron expression defining when the task should run
    /// </summary>
    public string CronExpression { get; set; } = string.Empty;

    /// <summary>
    /// Whether the task is enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// The next scheduled execution time (UTC)
    /// </summary>
    public DateTime? NextExecutionTime { get; set; }
}
