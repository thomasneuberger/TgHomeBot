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

## Example Tasks

### HourlyLogTask

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

### JackpotReportTask

A scheduled task that reports the current Eurojackpot lottery jackpot to Telegram every Tuesday and Friday at 10pm:

Configuration file (`JackpotReportTask.json`):
```json
{
  "taskType": "JackpotReportTask",
  "cronExpression": "0 22 * * 2,5",
  "enabled": true
}
```

The task fetches the latest Eurojackpot results from the Lottoland API and sends a formatted message to all registered Telegram chats, including:
- Last draw date, winning numbers, euro numbers, and jackpot amount
- Next draw date and expected jackpot (when available)

### MonthlyChargingReportTask

A scheduled task that sends a monthly charging report to Telegram on the first day of every month at midnight:

Configuration file (`MonthlyChargingReportTask.json`):
```json
{
  "taskType": "MonthlyChargingReportTask",
  "cronExpression": "0 0 1 * *",
  "enabled": true
}
```

The task generates a summary of EV charging sessions from the last two months, grouped by user and month. The report includes:
- Total kWh charged per user per month
- Formatted in German with month names
- Sent to all registered Telegram chats (respects the MonthlyChargingReport feature flag)

## Integration

To integrate the scheduler into your application:

```csharp
using TgHomeBot.Scheduling;

// In Program.cs or Startup.cs
builder.Services.AddScheduling();
```

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

## API Endpoints

The scheduler exposes REST API endpoints for monitoring and controlling scheduled tasks.

### List Scheduled Tasks

**GET** `/api/scheduler/tasks`

Returns information about all scheduled tasks, including disabled ones.

#### Response Example

```json
[
  {
    "taskType": "HourlyLogTask",
    "taskName": "HourlyLogTask",
    "cronExpression": "0 * * * *",
    "enabled": true,
    "nextExecutionTime": "2024-12-21T15:00:00Z"
  }
]
```

**Response Fields:**
- `taskType`: The type name of the task (used for configuration and execution)
- `taskName`: The display name of the task
- `cronExpression`: The cron expression defining when the task runs
- `enabled`: Whether the task is currently enabled
- `nextExecutionTime`: When the task will next execute (null for disabled tasks)

### Run Task Immediately

**POST** `/api/scheduler/tasks/run`

Executes a task immediately without changing its schedule. This endpoint can run even disabled tasks.

#### Request Body

```json
{
  "taskType": "HourlyLogTask"
}
```

#### Response

**Success (200 OK):**
```json
{
  "message": "Task HourlyLogTask executed successfully"
}
```

**Error (400 Bad Request):**
```json
{
  "message": "Failed to execute task HourlyLogTask. Check logs for details."
}
```

### API Testing

#### Using Swagger UI

1. Start the application: `dotnet run --project TgHomeBot.Api`
2. Open browser to: http://localhost:5271/swagger
3. Expand the "Scheduler" section
4. Try the endpoints

#### Using curl

List all tasks:
```bash
curl http://localhost:5271/api/scheduler/tasks
```

Run a task immediately:
```bash
curl -X POST http://localhost:5271/api/scheduler/tasks/run \
  -H "Content-Type: application/json" \
  -d '{"taskType": "HourlyLogTask"}'
```

#### Using the .http file

The `TgHomeBot.Api/TgHomeBot.Api.http` file contains example requests that can be used with REST clients like the VS Code REST Client extension.

