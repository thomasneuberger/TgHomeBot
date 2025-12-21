# TgHomeBot.Scheduling

This project provides scheduling functionality for the TgHomeBot application. It allows tasks to be executed at specific times based on cron expressions.

## Features

- **In-Memory Scheduling**: Tasks are scheduled in memory without requiring a database
- **Cron-Based Scheduling**: Uses cron expressions to define when tasks should run
- **JSON Configuration**: Each task is configured via a JSON file
- **Extensible Design**: Easy to add new tasks by implementing `IScheduledTask`
- **Hosted Service Integration**: Integrates seamlessly with ASP.NET Core's hosted service pattern

## Configuration

The scheduler is configured in `appsettings.json`:

```json
{
  "Scheduling": {
    "ConfigurationPath": "ScheduledTasks"
  }
}
```

The `ConfigurationPath` specifies the directory where task configuration files are located.

## Task Configuration

Each scheduled task requires a JSON configuration file in the configured directory. The file should have the following structure:

```json
{
  "taskType": "TaskTypeName",
  "cronExpression": "0 * * * *",
  "enabled": true
}
```

- **taskType**: The name of the task class (must be in the `TgHomeBot.Scheduling.Tasks` namespace)
- **cronExpression**: A cron expression defining when the task should run (format: `minute hour day month dayofweek`)
- **enabled**: Whether the task is enabled or disabled

### Cron Expression Format

The cron expression uses 5 fields:
```
minute hour day month dayofweek
```

Examples:
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

2. Create a configuration file in the `ScheduledTasks` directory (e.g., `MyCustomTask.json`):

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
builder.Services.AddScheduling(builder.Configuration);
```

This registers the scheduler as a hosted service that will start automatically when the application starts.

## Logging

The scheduler logs the following events:
- Scheduler startup and shutdown
- Task configuration loading
- Task execution start and completion
- Errors during task execution (tasks continue to run on schedule even after errors)

## Limitations

- The current cron expression parser is a simple implementation suitable for basic scheduling patterns
- For complex cron expressions or production use with demanding scheduling requirements, consider using a dedicated cron library
- The minute-by-minute search for next occurrence may be inefficient for some cron patterns
- Tasks run in-memory and are rescheduled on application restart based on configuration files
