using TgHomeBot.Scheduling.Contract.Models;

namespace TgHomeBot.Scheduling.Contract;

/// <summary>
/// Interface for the scheduler service that manages scheduled tasks
/// </summary>
public interface ISchedulerService
{
    /// <summary>
    /// Gets information about all scheduled tasks including disabled ones
    /// </summary>
    /// <returns>Collection of task information</returns>
    IEnumerable<ScheduledTaskInfo> GetScheduledTasks();

    /// <summary>
    /// Executes a task immediately by its type name
    /// </summary>
    /// <param name="taskType">The type name of the task</param>
    /// <returns>True if the task was executed successfully, false otherwise</returns>
    Task<bool> RunTaskNowAsync(string taskType);
}
