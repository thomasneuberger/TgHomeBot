namespace TgHomeBot.Scheduling;

/// <summary>
/// Interface for scheduled tasks
/// </summary>
public interface IScheduledTask
{
    /// <summary>
    /// The name of the task
    /// </summary>
    string TaskName { get; }

    /// <summary>
    /// Executes the task
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task ExecuteAsync(CancellationToken cancellationToken);
}
