using System.Collections.Concurrent;
using LumaLog.Abstractions;
using LumaLog.Models;

namespace LumaLog.Services;

/// <summary>
/// In-memory implementation of ILogStore for testing and development.
/// </summary>
public class InMemoryLogStore : ILogStore
{
    private readonly ConcurrentDictionary<long, LogEntry> _logs = new();
    private long _nextId = 1;

    public Task<long> InsertAsync(LogEntry entry, CancellationToken cancellationToken = default)
    {
        entry.Id = Interlocked.Increment(ref _nextId);
        _logs[entry.Id] = entry;
        return Task.FromResult(entry.Id);
    }

    public Task InsertBatchAsync(IEnumerable<LogEntry> entries, CancellationToken cancellationToken = default)
    {
        foreach (var entry in entries)
        {
            entry.Id = Interlocked.Increment(ref _nextId);
            _logs[entry.Id] = entry;
        }
        return Task.CompletedTask;
    }

    public Task<LogEntry?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        _logs.TryGetValue(id, out var entry);
        return Task.FromResult(entry);
    }

    public Task<List<LogEntry>> GetByTraceIdAsync(string traceId, CancellationToken cancellationToken = default)
    {
        var entries = _logs.Values
            .Where(e => e.TraceId == traceId)
            .OrderBy(e => e.CreatedAt)
            .ToList();
        return Task.FromResult(entries);
    }

    public Task<PagedResult<LogEntry>> QueryAsync(LogFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _logs.Values.AsEnumerable();

        if (filter.MinLevel.HasValue)
            query = query.Where(e => e.Level >= filter.MinLevel.Value);

        if (filter.MaxLevel.HasValue)
            query = query.Where(e => e.Level <= filter.MaxLevel.Value);

        if (filter.Levels != null && filter.Levels.Count > 0)
            query = query.Where(e => filter.Levels.Contains(e.Level));

        if (!string.IsNullOrEmpty(filter.SearchTerm))
            query = query.Where(e => e.Message.Contains(filter.SearchTerm, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(filter.TraceId))
            query = query.Where(e => e.TraceId == filter.TraceId);

        if (!string.IsNullOrEmpty(filter.UserId))
            query = query.Where(e => e.UserId == filter.UserId);

        if (!string.IsNullOrEmpty(filter.Source))
            query = query.Where(e => e.Source != null && e.Source.Contains(filter.Source, StringComparison.OrdinalIgnoreCase));

        if (filter.FromDate.HasValue)
            query = query.Where(e => e.CreatedAt >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(e => e.CreatedAt <= filter.ToDate.Value);

        if (filter.IsResolved.HasValue)
            query = query.Where(e => e.IsResolved == filter.IsResolved.Value);

        if (filter.HasException.HasValue)
            query = query.Where(e => filter.HasException.Value ? e.Exception != null : e.Exception == null);

        var total = query.Count();

        query = filter.SortDescending
            ? query.OrderByDescending(e => e.CreatedAt)
            : query.OrderBy(e => e.CreatedAt);

        var items = query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        return Task.FromResult(PagedResult<LogEntry>.Create(items, filter.Page, filter.PageSize, total));
    }

    public Task ResolveAsync(long id, string? resolvedBy = null, CancellationToken cancellationToken = default)
    {
        if (_logs.TryGetValue(id, out var entry))
        {
            entry.IsResolved = true;
            entry.ResolvedAt = DateTimeOffset.UtcNow;
            entry.ResolvedBy = resolvedBy;
        }
        return Task.CompletedTask;
    }

    public Task UnresolveAsync(long id, CancellationToken cancellationToken = default)
    {
        if (_logs.TryGetValue(id, out var entry))
        {
            entry.IsResolved = false;
            entry.ResolvedAt = null;
            entry.ResolvedBy = null;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        _logs.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<int> DeleteOlderThanAsync(DateTimeOffset date, CancellationToken cancellationToken = default)
    {
        var toRemove = _logs.Values.Where(e => e.CreatedAt < date).Select(e => e.Id).ToList();
        foreach (var id in toRemove)
        {
            _logs.TryRemove(id, out _);
        }
        return Task.FromResult(toRemove.Count);
    }

    public Task<LogStatistics> GetStatisticsAsync(DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default)
    {
        var query = _logs.Values.AsEnumerable();

        if (from.HasValue)
            query = query.Where(e => e.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.CreatedAt <= to.Value);

        var entries = query.ToList();

        var stats = new LogStatistics
        {
            TotalLogs = entries.Count,
            TotalErrors = entries.Count(e => e.Level == LogLevel.Error),
            TotalWarnings = entries.Count(e => e.Level == LogLevel.Warning),
            TotalCritical = entries.Count(e => e.Level == LogLevel.Critical),
            UnresolvedErrors = entries.Count(e => e.Level >= LogLevel.Error && !e.IsResolved),
            CountByLevel = entries.GroupBy(e => e.Level).ToDictionary(g => g.Key, g => (long)g.Count()),
            LastErrorTime = entries.Where(e => e.Level >= LogLevel.Error).OrderByDescending(e => e.CreatedAt).FirstOrDefault()?.CreatedAt
        };

        return Task.FromResult(stats);
    }

    public Task EnsureTablesExistAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
