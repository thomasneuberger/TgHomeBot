namespace TgHomeBot.Notifications.Contract;

public enum NotificationType
{
    /// <summary>
    /// General notification without specific feature flag filtering
    /// </summary>
    General,
    
    /// <summary>
    /// Eurojackpot lottery information
    /// </summary>
    Eurojackpot,
    
    /// <summary>
    /// Monthly charging report
    /// </summary>
    MonthlyChargingReport,
    
    /// <summary>
    /// Device state change notifications
    /// </summary>
    DeviceNotification
}
