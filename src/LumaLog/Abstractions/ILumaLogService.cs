using LumaLog.Models;

namespace LumaLog.Abstractions;

/// <summary>
/// Main service interface for LumaLog operations.
/// </summary>
public interface ILumaLogService
{
    /// <summary>
    /// Logs an entry.
    /// </summary>
    Task LogAsync(LogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs an error with exception details.
    /// </summary>
    Task LogErrorAsync(Exception exception, string? message = null, Dictionary<string, object>? customData = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a warning.
    /// </summary>
    Task LogWarningAsync(string message, Dictionary<string, object>? customData = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs an information message.
    /// </summary>
    Task LogInfoAsync(string message, Dictionary<string, object>? customData = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a log entry by ID.
    /// </summary>
    Task<LogEntry?> GetLogAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries logs with filtering.
    /// </summary>
    Task<PagedResult<LogEntry>> QueryLogsAsync(LogFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a trace summary.
    /// </summary>
    Task<TraceSummary?> GetTraceAsync(string traceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries traces with filtering.
    /// </summary>
    Task<PagedResult<TraceSummary>> QueryTracesAsync(TraceFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets log statistics.
    /// </summary>
    Task<LogStatistics> GetLogStatisticsAsync(DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets trace statistics.
    /// </summary>
    Task<TraceStatistics> GetTraceStatisticsAsync(DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a log entry.
    /// </summary>
    Task ResolveLogAsync(long id, string? resolvedBy = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a log entry.
    /// </summary>
    Task DeleteLogAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports logs using the specified exporter.
    /// </summary>
    Task<byte[]> ExportLogsAsync(LogFilter filter, string exporterName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports traces using the specified exporter.
    /// </summary>
    Task<byte[]> ExportTracesAsync(TraceFilter filter, string exporterName, CancellationToken cancellationToken = default);
}
