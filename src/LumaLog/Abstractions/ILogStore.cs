using LumaLog.Models;

namespace LumaLog.Abstractions;

/// <summary>
/// Contract for storing and retrieving log entries.
/// </summary>
public interface ILogStore
{
    /// <summary>
    /// Inserts a new log entry.
    /// </summary>
    Task<long> InsertAsync(LogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts multiple log entries in batch.
    /// </summary>
    Task InsertBatchAsync(IEnumerable<LogEntry> entries, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a log entry by its ID.
    /// </summary>
    Task<LogEntry?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets log entries by trace ID.
    /// </summary>
    Task<List<LogEntry>> GetByTraceIdAsync(string traceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries log entries with filtering and pagination.
    /// </summary>
    Task<PagedResult<LogEntry>> QueryAsync(LogFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a log entry as resolved.
    /// </summary>
    Task ResolveAsync(long id, string? resolvedBy = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unresolves a log entry.
    /// </summary>
    Task UnresolveAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a log entry.
    /// </summary>
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes log entries older than the specified date.
    /// </summary>
    Task<int> DeleteOlderThanAsync(DateTimeOffset date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets log statistics for the specified period.
    /// </summary>
    Task<LogStatistics> GetStatisticsAsync(DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures the required database tables exist.
    /// </summary>
    Task EnsureTablesExistAsync(CancellationToken cancellationToken = default);
}
