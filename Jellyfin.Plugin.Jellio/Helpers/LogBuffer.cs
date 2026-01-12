using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Plugin.Jellio.Helpers;

public static class LogBuffer
{
    private static readonly ConcurrentQueue<LogEntry> _logs = new();
    private const int _maxLogs = 500;

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
