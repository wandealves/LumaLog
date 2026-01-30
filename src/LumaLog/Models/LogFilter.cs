namespace LumaLog.Models;

/// <summary>
/// Represents filter criteria for querying logs.
/// </summary>
public class LogFilter
{
    public LogLevel? MinLevel { get; set; }

    public LogLevel? MaxLevel { get; set; }

    public List<LogLevel>? Levels { get; set; }

    public string? SearchTerm { get; set; }

    public string? TraceId { get; set; }

    public string? SpanId { get; set; }

    public string? UserId { get; set; }

    public string? Source { get; set; }

    public string? RequestPath { get; set; }

    public string? RequestMethod { get; set; }

    public int? StatusCode { get; set; }

    public string? MachineName { get; set; }

    public string? Environment { get; set; }

    public DateTimeOffset? FromDate { get; set; }

    public DateTimeOffset? ToDate { get; set; }

    public bool? IsResolved { get; set; }

    public bool? HasException { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 50;

    public string? SortBy { get; set; } = "CreatedAt";

    public bool SortDescending { get; set; } = true;
}

/// <summary>
/// Represents filter criteria for querying traces.
/// </summary>
public class TraceFilter
{
    public string? SearchTerm { get; set; }

    public string? ServiceName { get; set; }

    public SpanStatus? Status { get; set; }

    public long? MinDurationMs { get; set; }

    public long? MaxDurationMs { get; set; }

    public DateTimeOffset? FromDate { get; set; }

    public DateTimeOffset? ToDate { get; set; }

    public bool? HasErrors { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 50;

    public string? SortBy { get; set; } = "StartTime";

    public bool SortDescending { get; set; } = true;
}
