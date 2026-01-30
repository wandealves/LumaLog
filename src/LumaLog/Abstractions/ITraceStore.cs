using LumaLog.Models;

namespace LumaLog.Abstractions;

/// <summary>
/// Contract for storing and retrieving trace entries.
/// </summary>
public interface ITraceStore
{
    /// <summary>
    /// Inserts a new trace span.
    /// </summary>
    Task<long> InsertAsync(TraceEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts multiple trace spans in batch.
    /// </summary>
    Task InsertBatchAsync(IEnumerable<TraceEntry> entries, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a trace span by its ID.
    /// </summary>
    Task<TraceEntry?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a trace span by span ID.
    /// </summary>
    Task<TraceEntry?> GetBySpanIdAsync(string spanId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all spans for a trace.
    /// </summary>
    Task<List<TraceEntry>> GetByTraceIdAsync(string traceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a complete trace summary with all spans and related logs.
    /// </summary>
    Task<TraceSummary?> GetTraceSummaryAsync(string traceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries trace summaries with filtering and pagination.
    /// </summary>
    Task<PagedResult<TraceSummary>> QueryAsync(TraceFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a span's end time, duration, and status.
    /// </summary>
    Task CompleteSpanAsync(string spanId, DateTimeOffset endTime, SpanStatus status, string? statusMessage = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an event to a span.
    /// </summary>
    Task AddSpanEventAsync(string spanId, SpanEvent spanEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes spans older than the specified date.
    /// </summary>
    Task<int> DeleteOlderThanAsync(DateTimeOffset date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets trace statistics for the specified period.
    /// </summary>
    Task<TraceStatistics> GetStatisticsAsync(DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures the required database tables exist.
    /// </summary>
    Task EnsureTablesExistAsync(CancellationToken cancellationToken = default);
}
