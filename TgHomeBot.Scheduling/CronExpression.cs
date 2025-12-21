namespace TgHomeBot.Scheduling;

/// <summary>
/// Simple cron expression evaluator for basic scheduling patterns
/// Supports simplified cron expressions: "0 * * * *" for hourly, "0 0 * * *" for daily, etc.
/// Format: minute hour day month dayofweek
/// Note: dayofweek uses 0=Sunday convention (same as .NET DayOfWeek enum)
/// </summary>
public class CronExpression
{
    private const int MinutesPerHour = 60;
    private const int HoursPerDay = 24;
    private const int DaysToCheckAhead = 31;
    private const int MaxMinutesToCheck = MinutesPerHour * HoursPerDay * DaysToCheckAhead; // Check up to a month ahead
    
    private readonly string _expression;
    private readonly int? _minute;
    private readonly int? _hour;
    private readonly int? _day;
    private readonly int? _month;
    private readonly int? _dayOfWeek;

    public CronExpression(string expression)
    {
        _expression = expression ?? throw new ArgumentNullException(nameof(expression));
        var parts = expression.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length != 5)
        {
            throw new ArgumentException("Cron expression must have 5 parts: minute hour day month dayofweek", nameof(expression));
        }

        _minute = ParseField(parts[0]);
        _hour = ParseField(parts[1]);
        _day = ParseField(parts[2]);
        _month = ParseField(parts[3]);
        _dayOfWeek = ParseField(parts[4]);
    }

    private static int? ParseField(string field)
    {
        if (field == "*")
        {
            return null;
        }

        if (int.TryParse(field, out var value))
        {
            return value;
        }

        throw new ArgumentException($"Invalid cron field: {field}");
    }

    /// <summary>
    /// Determines if the cron expression matches the given time
    /// </summary>
    public bool Matches(DateTime time)
    {
        if (_minute.HasValue && time.Minute != _minute.Value)
            return false;

        if (_hour.HasValue && time.Hour != _hour.Value)
            return false;

        if (_day.HasValue && time.Day != _day.Value)
            return false;

        if (_month.HasValue && time.Month != _month.Value)
            return false;

        // Day of week: uses .NET DayOfWeek enum convention (0=Sunday, 1=Monday, ..., 6=Saturday)
        // This matches the behavior of many cron implementations
        if (_dayOfWeek.HasValue && (int)time.DayOfWeek != _dayOfWeek.Value)
            return false;

        return true;
    }

    /// <summary>
    /// Gets the next occurrence of this cron expression after the given time
    /// </summary>
    public DateTime GetNextOccurrence(DateTime after)
    {
        var next = after.AddMinutes(1);
        next = new DateTime(next.Year, next.Month, next.Day, next.Hour, next.Minute, 0, next.Kind);

        // Search for the next matching time
        // Note: This is a simple implementation; for production use, consider a dedicated cron library
        for (int i = 0; i < MaxMinutesToCheck; i++)
        {
            if (Matches(next))
            {
                return next;
            }
            next = next.AddMinutes(1);
        }

        throw new InvalidOperationException($"Could not find next occurrence for cron expression: {_expression}");
    }

    /// <summary>
    /// Gets the time until the next occurrence
    /// </summary>
    public TimeSpan GetTimeUntilNext(DateTime from)
    {
        var next = GetNextOccurrence(from);
        return next - from;
    }
}
