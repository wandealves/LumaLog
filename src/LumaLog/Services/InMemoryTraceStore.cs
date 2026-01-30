using System.Collections.Concurrent;
using LumaLog.Abstractions;
using LumaLog.Models;

namespace LumaLog.Services;

/// <summary>
/// In-memory implementation of ITraceStore for testing and development.
/// </summary>
public class InMemoryTraceStore : ITraceStore
{
    private readonly ConcurrentDictionary<long, TraceEntry> _traces = new();
    private readonly ILogStore? _logStore;
    private long _nextId = 1;

    public InMemoryTraceStore(ILogStore? logStore = null)
    {
        _logStore = logStore;
    }

    public Task<long> InsertAsync(TraceEntry entry, CancellationToken cancellationToken = default)
    {
        entry.Id = Interlocked.Increment(ref _nextId);
        _traces[entry.Id] = entry;
        return Task.FromResult(entry.Id);
    }

    public Task InsertBatchAsync(IEnumerable<TraceEntry> entries, CancellationToken cancellationToken = default)
    {
        foreach (var entry in entries)
        {
            entry.Id = Interlocked.Increment(ref _nextId);
            _traces[entry.Id] = entry;
        }
        return Task.CompletedTask;
    }

    public Task<TraceEntry?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        _traces.TryGetValue(id, out var entry);
        return Task.FromResult(entry);
    }

    public Task<TraceEntry?> GetBySpanIdAsync(string spanId, CancellationToken cancellationToken = default)
    {
        var entry = _traces.Values.FirstOrDefault(e => e.SpanId == spanId);
        return Task.FromResult(entry);
    }

    public Task<List<TraceEntry>> GetByTraceIdAsync(string traceId, CancellationToken cancellationToken = default)
    {
        var entries = _traces.Values
            .Where(e => e.TraceId == traceId)
            .OrderBy(e => e.StartTime)
            .ToList();
        return Task.FromResult(entries);
    }

    public async Task<TraceSummary?> GetTraceSummaryAsync(string traceId, CancellationToken cancellationToken = default)
    {
        var spans = await GetByTraceIdAsync(traceId, cancellationToken);
        if (spans.Count == 0) return null;

        var rootSpan = spans.FirstOrDefault(s => s.ParentSpanId == null) ?? spans.First();

        var summary = new TraceSummary
        {
            TraceId = traceId,
            RootSpanName = rootSpan.Name,
            StartTime = spans.Min(s => s.StartTime),
            EndTime = spans.Where(s => s.EndTime.HasValue).Select(s => s.EndTime!.Value).DefaultIfEmpty().Max(),
            SpanCount = spans.Count,
            ErrorCount = spans.Count(s => s.Status == SpanStatus.Error),
            OverallStatus = spans.Any(s => s.Status == SpanStatus.Error) ? SpanStatus.Error : SpanStatus.Ok,
            ServiceName = rootSpan.ServiceName,
            Spans = spans
        };

        if (summary.EndTime.HasValue)
        {
            summary.TotalDurationMs = (long)(summary.EndTime.Value - summary.StartTime).TotalMilliseconds;
        }

        if (_logStore != null)
        {
            summary.RelatedLogs = await _logStore.GetByTraceIdAsync(traceId, cancellationToken);
        }

        return summary;
    }

    public async Task<PagedResult<TraceSummary>> QueryAsync(TraceFilter filter, CancellationToken cancellationToken = default)
    {
        var traceGroups = _traces.Values
            .GroupBy(e => e.TraceId)
            .ToList();

        var summaries = new List<TraceSummary>();

        foreach (var group in traceGroups)
        {
            var spans = group.OrderBy(s => s.StartTime).ToList();
            var rootSpan = spans.FirstOrDefault(s => s.ParentSpanId == null) ?? spans.First();

            var summary = new TraceSummary
            {
                TraceId = group.Key,
                RootSpanName = rootSpan.Name,
                StartTime = spans.Min(s => s.StartTime),
                EndTime = spans.Where(s => s.EndTime.HasValue).Select(s => s.EndTime!.Value).DefaultIfEmpty().Max(),
                SpanCount = spans.Count,
                ErrorCount = spans.Count(s => s.Status == SpanStatus.Error),
                OverallStatus = spans.Any(s => s.Status == SpanStatus.Error) ? SpanStatus.Error : SpanStatus.Ok,
                ServiceName = rootSpan.ServiceName,
                Spans = spans
            };

            if (summary.EndTime.HasValue)
            {
                summary.TotalDurationMs = (long)(summary.EndTime.Value - summary.StartTime).TotalMilliseconds;
            }

            summaries.Add(summary);
        }

        var query = summaries.AsEnumerable();

        if (!string.IsNullOrEmpty(filter.SearchTerm))
            query = query.Where(s => s.RootSpanName?.Contains(filter.SearchTerm, StringComparison.OrdinalIgnoreCase) == true);

        if (!string.IsNullOrEmpty(filter.ServiceName))
            query = query.Where(s => s.ServiceName == filter.ServiceName);

        if (filter.Status.HasValue)
            query = query.Where(s => s.OverallStatus == filter.Status.Value);

        if (filter.MinDurationMs.HasValue)
            query = query.Where(s => s.TotalDurationMs >= filter.MinDurationMs.Value);

        if (filter.MaxDurationMs.HasValue)
            query = query.Where(s => s.TotalDurationMs <= filter.MaxDurationMs.Value);

        if (filter.FromDate.HasValue)
            query = query.Where(s => s.StartTime >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(s => s.StartTime <= filter.ToDate.Value);

        if (filter.HasErrors.HasValue)
            query = query.Where(s => filter.HasErrors.Value ? s.ErrorCount > 0 : s.ErrorCount == 0);

        var total = query.Count();

        query = filter.SortDescending
            ? query.OrderByDescending(s => s.StartTime)
            : query.OrderBy(s => s.StartTime);

        var items = query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        return PagedResult<TraceSummary>.Create(items, filter.Page, filter.PageSize, total);
    }

    public Task CompleteSpanAsync(string spanId, DateTimeOffset endTime, SpanStatus status, string? statusMessage = null, CancellationToken cancellationToken = default)
    {
        var entry = _traces.Values.FirstOrDefault(e => e.SpanId == spanId);
        if (entry != null)
        {
            entry.EndTime = endTime;
            entry.DurationMs = (long)(endTime - entry.StartTime).TotalMilliseconds;
            entry.Status = status;
            entry.StatusMessage = statusMessage;
        }
        return Task.CompletedTask;
    }

    public Task AddSpanEventAsync(string spanId, SpanEvent spanEvent, CancellationToken cancellationToken = default)
    {
        var entry = _traces.Values.FirstOrDefault(e => e.SpanId == spanId);
        if (entry != null)
        {
            entry.Events ??= new List<SpanEvent>();
            entry.Events.Add(spanEvent);
        }
        return Task.CompletedTask;
    }

    public Task<int> DeleteOlderThanAsync(DateTimeOffset date, CancellationToken cancellationToken = default)
    {
        var toRemove = _traces.Values.Where(e => e.CreatedAt < date).Select(e => e.Id).ToList();
        foreach (var id in toRemove)
        {
            _traces.TryRemove(id, out _);
        }
        return Task.FromResult(toRemove.Count);
    }

    public Task<TraceStatistics> GetStatisticsAsync(DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default)
    {
        var query = _traces.Values.AsEnumerable();

        if (from.HasValue)
            query = query.Where(e => e.StartTime >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.StartTime <= to.Value);

        var entries = query.ToList();
        var traceIds = entries.Select(e => e.TraceId).Distinct().Count();

        var stats = new TraceStatistics
        {
            TotalTraces = traceIds,
            TotalSpans = entries.Count,
            ErrorTraces = entries.GroupBy(e => e.TraceId).Count(g => g.Any(e => e.Status == SpanStatus.Error)),
            AverageDurationMs = entries.Where(e => e.DurationMs.HasValue).Select(e => e.DurationMs!.Value).DefaultIfEmpty().Average(),
            MaxDurationMs = entries.Where(e => e.DurationMs.HasValue).Select(e => e.DurationMs!.Value).DefaultIfEmpty().Max(),
            MinDurationMs = entries.Where(e => e.DurationMs.HasValue).Select(e => e.DurationMs!.Value).DefaultIfEmpty().Min()
        };

        return Task.FromResult(stats);
    }

    public Task EnsureTablesExistAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
