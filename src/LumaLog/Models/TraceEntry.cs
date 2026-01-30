using System.Text.Json;

namespace LumaLog.Models;

/// <summary>
/// Represents a span in a distributed trace.
/// </summary>
public class TraceEntry
{
    public long Id { get; set; }

    public string TraceId { get; set; } = string.Empty;

    public string SpanId { get; set; } = string.Empty;

    public string? ParentSpanId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset? EndTime { get; set; }

    public long? DurationMs { get; set; }

    public SpanStatus Status { get; set; } = SpanStatus.Unset;

    public string? StatusMessage { get; set; }

    public Dictionary<string, string>? Tags { get; set; }

    public string? TagsJson
    {
        get => Tags != null ? JsonSerializer.Serialize(Tags) : null;
        set => Tags = value != null
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(value)
            : null;
    }

    public List<SpanEvent>? Events { get; set; }

    public string? EventsJson
    {
        get => Events != null ? JsonSerializer.Serialize(Events) : null;
        set => Events = value != null
            ? JsonSerializer.Deserialize<List<SpanEvent>>(value)
            : null;
    }

    public string? ServiceName { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents an event that occurred during a span.
/// </summary>
public class SpanEvent
{
    public string Name { get; set; } = string.Empty;

    public DateTimeOffset Timestamp { get; set; }

    public Dictionary<string, string>? Attributes { get; set; }
}
