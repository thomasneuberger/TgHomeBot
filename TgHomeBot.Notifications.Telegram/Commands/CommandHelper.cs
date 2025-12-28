namespace TgHomeBot.Notifications.Telegram.Commands;

/// <summary>
/// Helper methods for Telegram command processing
/// </summary>
internal static class CommandHelper
{
    /// <summary>
    /// Removes the bot name suffix from a parameter (e.g., "TaskName@botname" -> "TaskName")
    /// This is necessary because Telegram appends @botname to commands in group chats
    /// </summary>
    /// <param name="parameter">The parameter string that may contain a @botname suffix</param>
    /// <returns>The parameter string with the @botname suffix removed if present</returns>
    public static string StripBotName(string parameter)
    {
        if (string.IsNullOrEmpty(parameter))
        {
            return parameter;
        }

        var atIndex = parameter.IndexOf('@');
        return atIndex > 0 ? parameter[..atIndex] : parameter;
    }
}
