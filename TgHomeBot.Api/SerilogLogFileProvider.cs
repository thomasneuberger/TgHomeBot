using System.Text;
using Microsoft.Extensions.Options;
using Serilog;
using TgHomeBot.Common.Contract;

namespace TgHomeBot.Api;

public class SerilogLogFileProvider(
    IOptions<SerilogLogFileProvider.SerilogOptions> serilogOptions,
    ILogger<SerilogLogFileProvider> logger)
    : ILogFileProvider
{
    private readonly SerilogOptions _serilogOptions = serilogOptions.Value;

    public IReadOnlyList<string> GetLogFileList()
    {
        var folder = GetLogFilePath();
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return [];
        }

        return Directory.GetFiles(folder).Select(f => Path.GetFileName(f)!).ToList();
    }

    public Stream? GetLogFileContent(string filename, CancellationToken cancellationToken)
    {
        var path = GetLogFilePath();
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }
        filename = Path.Combine(path, filename);
        if (!File.Exists(filename))
        {
            return null;
        }

        var l = Log.Logger;

        try
        {
            return new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error reading log file: {Exception}", e.Message);
            return null;
        }
    }

    private string? GetLogFilePath() =>
        _serilogOptions.WriteTo.FirstOrDefault(w => w.Name == "File")?.Args.TryGetValue("path", out var path) == true
            ? Path.GetDirectoryName(path)
            : null;

    public class SerilogOptions
    {
        public required WriteToSettings[] WriteTo { get; init; }
    }

    public class WriteToSettings
    {
        public required string Name { get; set; }
        public IDictionary<string, string> Args { get; set; } = new Dictionary<string, string>();
    }
}
