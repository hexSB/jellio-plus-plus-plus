using System;

namespace Jellyfin.Plugin.Jellio.Helpers;

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Message { get; set; } = string.Empty;
    public LogLevel Level { get; set; }
}

