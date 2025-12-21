# TgHomeBot.Scheduling

This project provides scheduling functionality for the TgHomeBot application. It allows tasks to be executed at specific times based on cron expressions.

## Features

- **In-Memory Scheduling**: Tasks are scheduled in memory without requiring a database
- **Cron-Based Scheduling**: Uses [Cronos](https://github.com/HangfireIO/Cronos) library for robust cron expression parsing
- **JSON Configuration**: Each task is configured via a JSON file
- **Extensible Design**: Easy to add new tasks by implementing `IScheduledTask`
- **Hosted Service Integration**: Integrates seamlessly with ASP.NET Core's hosted service pattern
- **Fire-and-Forget**: Uses [AsyncAwaitBestPractices](https://github.com/brminnick/AsyncAwaitBestPractices) for safe background task execution

## Configuration

The scheduler uses the `FileStorage` path configuration from `appsettings.json`. Task configuration files should be placed in a `ScheduledTasks` subdirectory within the configured FileStorage path:

```json
{
  "FileStorage": {
    "Path": "/path/to/storage"
  }
}
```

Task configurations will be read from: `/path/to/storage/ScheduledTasks/*.json`

## Task Configuration

Each scheduled task requires a JSON configuration file in the `ScheduledTasks` directory. The file should have the following structure:

```json
{
  "taskType": "TaskTypeName",
  "cronExpression": "0 * * * *",
  "enabled": true
}
```

- **taskType**: The name of the task class (must be in the `TgHomeBot.Scheduling.Tasks` namespace)
- **cronExpression**: A standard cron expression defining when the task should run
- **enabled**: Whether the task is enabled or disabled

### Cron Expression Format

The scheduler uses the Cronos library which supports standard cron expressions with 5 fields:
```
minute hour day month dayofweek
```

Examples:
- `0 * * * *` - Every hour at minute 0
- `0 0 * * *` - Every day at midnight
- `*/15 * * * *` - Every 15 minutes
- `0 9 * * 1` - Every Monday at 9:00 AM
- `0 0 1 * *` - First day of every month at midnight

For more complex patterns, see the [Cronos documentation](https://github.com/HangfireIO/Cronos).

- `0 * * * *` - Every hour at minute 0
- `0 0 * * *` - Every day at midnight
- `*/15 * * * *` - Every 15 minutes (Note: current implementation supports wildcards but not step values like */15)
- `0 9 * * 1` - Every Monday at 9:00 AM

**Note**: Day of week uses 0=Sunday convention (same as .NET's DayOfWeek enum)

## Creating a New Task

To create a new scheduled task:

1. Create a new class in the `TgHomeBot.Scheduling.Tasks` namespace that implements `IScheduledTask`:

```csharp
using Microsoft.Extensions.Logging;

namespace TgHomeBot.Scheduling.Tasks;

public class MyCustomTask : IScheduledTask
{
    private readonly ILogger<MyCustomTask> _logger;

    public string TaskName => "MyCustomTask";

    public MyCustomTask(ILogger<MyCustomTask> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MyCustomTask is executing");
        // Your task logic here
        return Task.CompletedTask;
    }
}
```

2. Create a configuration file in the `ScheduledTasks` subdirectory within your FileStorage path (e.g., `/path/to/storage/ScheduledTasks/MyCustomTask.json`):

```json
{
  "taskType": "MyCustomTask",
  "cronExpression": "0 0 * * *",
  "enabled": true
}
```

3. The task will automatically be discovered and scheduled when the application starts.

## Example: HourlyLogTask

The project includes a demonstration task `HourlyLogTask` that logs the current date and time every hour:

```csharp
public class HourlyLogTask : IScheduledTask
{
    private readonly ILogger<HourlyLogTask> _logger;

    public string TaskName => "HourlyLogTask";

    public HourlyLogTask(ILogger<HourlyLogTask> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var currentTime = DateTime.UtcNow;
        _logger.LogInformation("HourlyLogTask executed at: {DateTime}", 
            currentTime.ToString("yyyy-MM-dd HH:mm:ss"));
        return Task.CompletedTask;
    }
}
```

Configuration file (`HourlyLogTask.json`):
```json
{
  "taskType": "HourlyLogTask",
  "cronExpression": "0 * * * *",
  "enabled": true
}
```

## Integration

To integrate the scheduler into your application:

```csharp
using TgHomeBot.Scheduling;

// In Program.cs or Startup.cs
builder.Services.AddScheduling();
```

This registers the scheduler as a hosted service that will start automatically when the application starts. Make sure the `FileStorageOptions` is configured in your `appsettings.json`.

This registers the scheduler as a hosted service that will start automatically when the application starts. Make sure the `FileStorageOptions` is configured in your `appsettings.json`.

## Logging

The scheduler logs the following events:
- Scheduler startup and shutdown
- Task configuration loading
- Task execution start and completion
- Errors during task execution (tasks continue to run on schedule even after errors)

## Dependencies

This project uses the following external libraries:
- **[Cronos](https://github.com/HangfireIO/Cronos)** (MIT License) - Robust cron expression parser
- **[AsyncAwaitBestPractices](https://github.com/brminnick/AsyncAwaitBestPractices)** (MIT License) - Safe fire-and-forget async patterns

## Technical Notes

- Tasks run in-memory and are rescheduled on application restart based on configuration files
- The Cronos library provides efficient and accurate cron expression parsing
- Fire-and-forget task execution uses AsyncAwaitBestPractices to avoid common async pitfalls
- All times are handled in UTC to ensure consistent scheduling across timezones

