namespace LumaLog.Models;

/// <summary>
/// Represents a summary of a complete trace with all its spans.
/// </summary>
public class TraceSummary
{
    public string TraceId { get; set; } = string.Empty;

    public string? RootSpanName { get; set; }

    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset? EndTime { get; set; }

    public long? TotalDurationMs { get; set; }

    public int SpanCount { get; set; }

    public int ErrorCount { get; set; }

    public SpanStatus OverallStatus { get; set; }

    public string? ServiceName { get; set; }

    public List<TraceEntry> Spans { get; set; } = new();

    public List<LogEntry> RelatedLogs { get; set; } = new();
}
