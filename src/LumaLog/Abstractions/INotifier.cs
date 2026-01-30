using LumaLog.Models;

namespace LumaLog.Abstractions;

/// <summary>
/// Contract for sending notifications about log events.
/// </summary>
public interface INotifier
{
    /// <summary>
    /// Gets the unique name of this notifier.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Sends a notification for a log entry.
    /// </summary>
    Task NotifyAsync(LogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a batch notification for multiple log entries.
    /// </summary>
    Task NotifyBatchAsync(IEnumerable<LogEntry> entries, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether this notifier should handle the given log entry.
    /// </summary>
    bool ShouldNotify(LogEntry entry);
}

/// <summary>
/// Base class for notifiers with common filtering logic.
/// </summary>
public abstract class NotifierBase : INotifier
{
    public abstract string Name { get; }

    public LogLevel MinLevel { get; set; } = LogLevel.Error;

    public List<LogLevel>? OnlyLevels { get; set; }

    public List<string>? IncludeSources { get; set; }

    public List<string>? ExcludeSources { get; set; }

    public abstract Task NotifyAsync(LogEntry entry, CancellationToken cancellationToken = default);

    public virtual async Task NotifyBatchAsync(IEnumerable<LogEntry> entries, CancellationToken cancellationToken = default)
    {
        foreach (var entry in entries.Where(ShouldNotify))
        {
            await NotifyAsync(entry, cancellationToken);
        }
    }

    public virtual bool ShouldNotify(LogEntry entry)
    {
        if (OnlyLevels != null && OnlyLevels.Count > 0)
        {
            if (!OnlyLevels.Contains(entry.Level))
                return false;
        }
        else if (entry.Level < MinLevel)
        {
            return false;
        }

        if (IncludeSources != null && IncludeSources.Count > 0)
        {
            if (entry.Source == null || !IncludeSources.Any(s => entry.Source.Contains(s, StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        if (ExcludeSources != null && ExcludeSources.Count > 0)
        {
            if (entry.Source != null && ExcludeSources.Any(s => entry.Source.Contains(s, StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        return true;
    }
}
