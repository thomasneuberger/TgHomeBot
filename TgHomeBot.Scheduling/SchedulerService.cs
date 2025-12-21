using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace TgHomeBot.Scheduling;

/// <summary>
/// Hosted service that manages scheduled tasks
/// </summary>
public class SchedulerService : IHostedService, IDisposable
{
    private readonly ILogger<SchedulerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly SchedulingOptions _options;
    private readonly List<ScheduledTaskRunner> _taskRunners = new();
    private readonly CancellationTokenSource _stoppingCts = new();

    public SchedulerService(
        ILogger<SchedulerService> logger,
        IServiceProvider serviceProvider,
        IOptions<SchedulingOptions> options)
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
                        _ = runner.StartAsync(_stoppingCts.Token); // Fire and forget
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

        if (string.IsNullOrEmpty(_options.ConfigurationPath))
        {
            _logger.LogWarning("Configuration path is not set, no tasks will be scheduled");
            return configurations;
        }

        if (!Directory.Exists(_options.ConfigurationPath))
        {
            _logger.LogWarning("Configuration directory does not exist: {Path}", _options.ConfigurationPath);
            return configurations;
        }

        var configFiles = Directory.GetFiles(_options.ConfigurationPath, "*.json");
        _logger.LogInformation("Found {Count} configuration file(s) in {Path}", configFiles.Length, _options.ConfigurationPath);

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
        // Get the task type from the current assembly
        var type = Type.GetType($"TgHomeBot.Scheduling.Tasks.{taskType}, TgHomeBot.Scheduling");
        
        if (type == null)
        {
            _logger.LogError("Task type not found: {TaskType}", taskType);
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

        public ScheduledTaskRunner(IScheduledTask task, TaskConfiguration configuration, ILogger logger)
        {
            _task = task ?? throw new ArgumentNullException(nameof(task));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cronExpression = new CronExpression(configuration.CronExpression);
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
                    var now = DateTime.Now;
                    var delay = _cronExpression.GetTimeUntilNext(now);

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
