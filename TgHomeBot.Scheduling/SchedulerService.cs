using AsyncAwaitBestPractices;
using Cronos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TgHomeBot.Common.Contract;

namespace TgHomeBot.Scheduling;

/// <summary>
/// Hosted service that manages scheduled tasks
/// </summary>
public class SchedulerService : IHostedService, IDisposable
{
    private readonly ILogger<SchedulerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly FileStorageOptions _options;
    private readonly List<ScheduledTaskRunner> _taskRunners = new();
    private readonly CancellationTokenSource _stoppingCts = new();

    public SchedulerService(
        ILogger<SchedulerService> logger,
        IServiceProvider serviceProvider,
        IOptions<FileStorageOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting scheduler service");

        try
        {
            // Load task configurations from the configuration directory
            var configurations = LoadTaskConfigurations();
            _logger.LogInformation("Loaded {Count} task configuration(s)", configurations.Count);

            // Create and start task runners
            foreach (var config in configurations)
            {
                if (!config.Enabled)
                {
                    _logger.LogInformation("Task {TaskType} is disabled, skipping", config.TaskType);
                    continue;
                }

                try
                {
                    var task = CreateTask(config.TaskType);
                    if (task != null)
                    {
                        var runner = new ScheduledTaskRunner(task, config, _logger);
                        _taskRunners.Add(runner);
                        runner.StartAsync(_stoppingCts.Token).SafeFireAndForget();
                        _logger.LogInformation("Started task runner for {TaskName} with schedule {CronExpression}", 
                            task.TaskName, config.CronExpression);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create task {TaskType}", config.TaskType);
                }
            }

            _logger.LogInformation("Scheduler service started with {Count} active task(s)", _taskRunners.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start scheduler service");
            throw;
        }

        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping scheduler service");
        
        _stoppingCts.Cancel();

        // Wait for all tasks to complete
        await Task.WhenAll(_taskRunners.Select(r => r.StopAsync()));

        _logger.LogInformation("Scheduler service stopped");
    }

    private List<TaskConfiguration> LoadTaskConfigurations()
    {
        var configurations = new List<TaskConfiguration>();

        if (string.IsNullOrEmpty(_options.Path))
        {
            _logger.LogWarning("FileStorage path is not set, no tasks will be scheduled");
            return configurations;
        }

        var scheduledTasksPath = Path.Combine(_options.Path, "ScheduledTasks");
        
        if (!Directory.Exists(scheduledTasksPath))
        {
            _logger.LogWarning("ScheduledTasks directory does not exist: {Path}", scheduledTasksPath);
            return configurations;
        }

        var configFiles = Directory.GetFiles(scheduledTasksPath, "*.json");
        _logger.LogInformation("Found {Count} configuration file(s) in {Path}", configFiles.Length, scheduledTasksPath);

        foreach (var file in configFiles)
        {
            try
            {
                var json = File.ReadAllText(file);
                var config = JsonSerializer.Deserialize<TaskConfiguration>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (config != null)
                {
                    configurations.Add(config);
                    _logger.LogDebug("Loaded configuration from {File}: {TaskType}", file, config.TaskType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load configuration from {File}", file);
            }
        }

        return configurations;
    }

    private IScheduledTask? CreateTask(string taskType)
    {
        // Try to find the task type in the current assembly
        var assembly = typeof(SchedulerService).Assembly;
        var fullTypeName = $"TgHomeBot.Scheduling.Tasks.{taskType}";
        var type = assembly.GetType(fullTypeName);
        
        if (type == null)
        {
            _logger.LogError("Task type not found: {TaskType} (looking for {FullTypeName})", taskType, fullTypeName);
            return null;
        }

        if (!typeof(IScheduledTask).IsAssignableFrom(type))
        {
            _logger.LogError("Type {TaskType} does not implement IScheduledTask", taskType);
            return null;
        }

        try
        {
            // Try to create instance with DI
            var task = ActivatorUtilities.CreateInstance(_serviceProvider, type) as IScheduledTask;
            return task;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create instance of task {TaskType}", taskType);
            return null;
        }
    }

    /// <summary>
    /// Gets information about all scheduled tasks including disabled ones
    /// </summary>
    /// <returns>Collection of task information</returns>
    public IEnumerable<(string TaskType, string TaskName, string CronExpression, bool Enabled, DateTime? NextExecutionTime)> GetScheduledTasks()
    {
        var configurations = LoadTaskConfigurations();
        var result = new List<(string, string, string, bool, DateTime?)>();

        foreach (var config in configurations)
        {
            DateTime? nextExecution = null;
            string taskName = config.TaskType;

            // Find the running task runner if it exists
            var runner = _taskRunners.FirstOrDefault(r => r.TaskType == config.TaskType);
            if (runner != null)
            {
                taskName = runner.TaskName;
                nextExecution = runner.GetNextExecutionTime();
            }
            else if (config.Enabled)
            {
                // If enabled but not in runners, try to get task name from task creation
                var task = CreateTask(config.TaskType);
                if (task != null)
                {
                    taskName = task.TaskName;
                    try
                    {
                        var cronExpression = CronExpression.Parse(config.CronExpression, CronFormat.Standard);
                        nextExecution = cronExpression.GetNextOccurrence(DateTime.UtcNow, TimeZoneInfo.Utc);
                    }
                    catch
                    {
                        // Ignore cron parsing errors
                    }
                }
            }

            result.Add((config.TaskType, taskName, config.CronExpression, config.Enabled, nextExecution));
        }

        return result;
    }

    /// <summary>
    /// Executes a task immediately by its type name
    /// </summary>
    /// <param name="taskType">The type name of the task</param>
    /// <returns>True if the task was executed successfully, false otherwise</returns>
    public async Task<bool> RunTaskNowAsync(string taskType)
    {
        try
        {
            var task = CreateTask(taskType);
            if (task == null)
            {
                _logger.LogWarning("Task type {TaskType} not found or could not be created", taskType);
                return false;
            }

            _logger.LogInformation("Running task {TaskName} ({TaskType}) on demand", task.TaskName, taskType);
            await task.ExecuteAsync(CancellationToken.None);
            _logger.LogInformation("Task {TaskName} ({TaskType}) completed successfully", task.TaskName, taskType);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running task {TaskType} on demand", taskType);
            return false;
        }
    }

    public void Dispose()
    {
        _stoppingCts.Cancel();
        _stoppingCts.Dispose();
    }

    /// <summary>
    /// Internal class to run a scheduled task
    /// </summary>
    private class ScheduledTaskRunner
    {
        private readonly IScheduledTask _task;
        private readonly TaskConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly CronExpression _cronExpression;
        private Task? _runningTask;

        public string TaskType => _configuration.TaskType;
        public string TaskName => _task.TaskName;

        public ScheduledTaskRunner(IScheduledTask task, TaskConfiguration configuration, ILogger logger)
        {
            _task = task ?? throw new ArgumentNullException(nameof(task));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cronExpression = CronExpression.Parse(configuration.CronExpression, CronFormat.Standard);
        }

        public DateTime? GetNextExecutionTime()
        {
            return _cronExpression.GetNextOccurrence(DateTime.UtcNow, TimeZoneInfo.Utc);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _runningTask = RunAsync(cancellationToken);
            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            if (_runningTask != null)
            {
                await _runningTask;
            }
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Task runner started for {TaskName}", _task.TaskName);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Calculate time until next execution
                    var now = DateTime.UtcNow;
                    var nextOccurrence = _cronExpression.GetNextOccurrence(now, TimeZoneInfo.Utc);
                    if (nextOccurrence == null)
                    {
                        _logger.LogWarning("Could not calculate next occurrence for task {TaskName}", _task.TaskName);
                        break;
                    }
                    
                    var delay = nextOccurrence.Value - now;

                    _logger.LogDebug("Task {TaskName} will run in {Delay}", _task.TaskName, delay);

                    // Wait until the next execution time
                    await Task.Delay(delay, cancellationToken);

                    if (cancellationToken.IsCancellationRequested)
                        break;

                    // Execute the task
                    _logger.LogInformation("Executing task {TaskName}", _task.TaskName);
                    await _task.ExecuteAsync(cancellationToken);
                    _logger.LogInformation("Task {TaskName} completed successfully", _task.TaskName);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing task {TaskName}", _task.TaskName);
                    // Continue running despite errors
                }
            }

            _logger.LogInformation("Task runner stopped for {TaskName}", _task.TaskName);
        }
    }
}
