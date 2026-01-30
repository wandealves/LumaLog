namespace LumaLog.Models;

/// <summary>
/// Represents aggregated statistics for logs.
/// </summary>
public class LogStatistics
{
    public long TotalLogs { get; set; }

    public long TotalErrors { get; set; }

    public long TotalWarnings { get; set; }

    public long TotalCritical { get; set; }

    public long UnresolvedErrors { get; set; }

    public Dictionary<LogLevel, long> CountByLevel { get; set; } = new();

    public List<LogCountByPeriod> CountByHour { get; set; } = new();

    public List<LogCountByPeriod> CountByDay { get; set; } = new();

    public List<TopErrorSource> TopErrorSources { get; set; } = new();

    public List<TopErrorEndpoint> TopErrorEndpoints { get; set; } = new();

    public DateTimeOffset? LastErrorTime { get; set; }

    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents log count for a specific time period.
/// </summary>
public class LogCountByPeriod
{
    public DateTimeOffset Period { get; set; }

    public long Count { get; set; }

    public long ErrorCount { get; set; }

    public long WarningCount { get; set; }
}

/// <summary>
/// Represents a top error source with count.
/// </summary>
public class TopErrorSource
{
    public string Source { get; set; } = string.Empty;

    public long ErrorCount { get; set; }

    public DateTimeOffset? LastOccurrence { get; set; }
}

/// <summary>
/// Represents a top error endpoint with count.
/// </summary>
public class TopErrorEndpoint
{
    public string Endpoint { get; set; } = string.Empty;

    public string? Method { get; set; }

    public long ErrorCount { get; set; }

    public DateTimeOffset? LastOccurrence { get; set; }
}

/// <summary>
/// Represents aggregated statistics for traces.
/// </summary>
public class TraceStatistics
{
    public long TotalTraces { get; set; }

    public long TotalSpans { get; set; }

    public long ErrorTraces { get; set; }

    public double AverageDurationMs { get; set; }

    public long MaxDurationMs { get; set; }

    public long MinDurationMs { get; set; }

    public List<TraceCountByPeriod> CountByHour { get; set; } = new();

    public List<ServiceTraceStats> ByService { get; set; } = new();

    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents trace count for a specific time period.
/// </summary>
public class TraceCountByPeriod
{
    public DateTimeOffset Period { get; set; }

    public long Count { get; set; }

    public long ErrorCount { get; set; }

    public double AverageDurationMs { get; set; }
}

/// <summary>
/// Represents trace statistics for a specific service.
/// </summary>
public class ServiceTraceStats
{
    public string ServiceName { get; set; } = string.Empty;

    public long TraceCount { get; set; }

    public long ErrorCount { get; set; }

    public double AverageDurationMs { get; set; }
}
