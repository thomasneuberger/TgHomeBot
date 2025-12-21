namespace TgHomeBot.Scheduling;

/// <summary>
/// Options for the scheduling system
/// </summary>
public class SchedulingOptions
{
    /// <summary>
    /// Path to the directory containing task configuration files
    /// </summary>
    public string ConfigurationPath { get; set; } = string.Empty;
}
