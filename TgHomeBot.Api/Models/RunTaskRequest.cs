namespace TgHomeBot.Api.Models;

/// <summary>
/// Request to run a task immediately
/// </summary>
public class RunTaskRequest
{
    /// <summary>
    /// The type name of the task to run
    /// </summary>
    public string TaskType { get; set; } = string.Empty;
}
