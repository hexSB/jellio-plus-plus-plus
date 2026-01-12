using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Plugin.Jellio.Helpers;

public static class LogBuffer
{
    private static readonly ConcurrentQueue<LogEntry> _logs = new();
    private static readonly int _maxLogs = 500;

    public static void AddLog(string message, LogLevel level = LogLevel.Info)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Message = message,
            Level = level
        };

        _logs.Enqueue(entry);

        // Keep only the most recent logs
        while (_logs.Count > _maxLogs)
        {
            _logs.TryDequeue(out _);
        }
    }

    public static List<LogEntry> GetLogs(int? limit = null)
    {
        var logs = _logs.ToList();
        if (limit.HasValue && limit.Value > 0)
        {
            logs = logs.TakeLast(limit.Value).ToList();
        }
        return logs;
    }

    public static void Clear()
    {
        while (_logs.TryDequeue(out _)) { }
    }
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Message { get; set; } = string.Empty;
    public LogLevel Level { get; set; }
}

public enum LogLevel
{
    Info,
    Warning,
    Error
}

