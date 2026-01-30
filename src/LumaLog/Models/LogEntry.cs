using System.Text.Json;

namespace LumaLog.Models;

/// <summary>
/// Represents a log entry with all associated metadata.
/// </summary>
public class LogEntry
{
    public long Id { get; set; }

    public LogLevel Level { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? Exception { get; set; }

    public string? StackTrace { get; set; }

    public string? Source { get; set; }

    public string? TraceId { get; set; }

    public string? SpanId { get; set; }

    public string? ParentSpanId { get; set; }

    public string? UserId { get; set; }

    public string? UserName { get; set; }

    public string? IpAddress { get; set; }

    public string? RequestPath { get; set; }

    public string? RequestMethod { get; set; }

    public int? StatusCode { get; set; }

    public string? MachineName { get; set; }

    public string? Environment { get; set; }

    public Dictionary<string, object>? CustomData { get; set; }

    public string? CustomDataJson
    {
        get => CustomData != null ? JsonSerializer.Serialize(CustomData) : null;
        set => CustomData = value != null
            ? JsonSerializer.Deserialize<Dictionary<string, object>>(value)
            : null;
    }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public bool IsResolved { get; set; }

    public DateTimeOffset? ResolvedAt { get; set; }

    public string? ResolvedBy { get; set; }
}
