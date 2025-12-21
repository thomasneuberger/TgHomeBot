namespace TgHomeBot.Scheduling;

/// <summary>
/// Configuration for a scheduled task
/// </summary>
public class TaskConfiguration
{
    /// <summary>
    /// The name of the task type
    /// </summary>
    public string TaskType { get; set; } = string.Empty;

    /// <summary>
    /// The cron expression defining when the task should run
    /// </summary>
    public string CronExpression { get; set; } = string.Empty;

    /// <summary>
    /// Whether the task is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;
}
