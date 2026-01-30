using LumaLog.Abstractions;
using LumaLog.Configuration;
using LumaLog.Models;
using Microsoft.Extensions.Options;

namespace LumaLog.Services;

/// <summary>
/// Main service implementation for LumaLog operations.
/// </summary>
public class LumaLogService : ILumaLogService
{
    private readonly ILogStore _logStore;
    private readonly ITraceStore? _traceStore;
    private readonly ITraceManager _traceManager;
    private readonly IEnumerable<INotifier> _notifiers;
    private readonly IEnumerable<IExporter> _exporters;
    private readonly LumaLogOptions _options;

    public LumaLogService(
        ILogStore logStore,
        ITraceManager traceManager,
        IOptions<LumaLogOptions> options,
        ITraceStore? traceStore = null,
        IEnumerable<INotifier>? notifiers = null,
        IEnumerable<IExporter>? exporters = null)
    {
        _logStore = logStore;
        _traceStore = traceStore;
        _traceManager = traceManager;
        _notifiers = notifiers ?? Enumerable.Empty<INotifier>();
        _exporters = exporters ?? Enumerable.Empty<IExporter>();
        _options = options.Value;
    }

    public async Task LogAsync(LogEntry entry, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled) return;
        if (entry.Level < _options.MinimumLevel) return;

        // Enrich with trace context
        if (string.IsNullOrEmpty(entry.TraceId))
        {
            entry.TraceId = _traceManager.CurrentTraceId;
        }
        if (string.IsNullOrEmpty(entry.SpanId))
        {
            entry.SpanId = _traceManager.CurrentSpanId;
        }
        if (string.IsNullOrEmpty(entry.ParentSpanId))
        {
            entry.ParentSpanId = _traceManager.CurrentParentSpanId;
        }

        // Enrich with environment info
        if (string.IsNullOrEmpty(entry.Environment))
        {
            entry.Environment = _options.Environment;
        }
        if (_options.IncludeMachineName && string.IsNullOrEmpty(entry.MachineName))
        {
            entry.MachineName = System.Environment.MachineName;
        }

        // Persist
        await _logStore.InsertAsync(entry, cancellationToken);

        // Notify
        foreach (var notifier in _notifiers.Where(n => n.ShouldNotify(entry)))
        {
            try
            {
                await notifier.NotifyAsync(entry, cancellationToken);
            }
            catch
            {
                // Don't let notification failures affect the main flow
            }
        }
    }

    public async Task LogErrorAsync(Exception exception, string? message = null, Dictionary<string, object>? customData = null, CancellationToken cancellationToken = default)
    {
        var entry = new LogEntry
        {
            Level = LogLevel.Error,
            Message = message ?? exception.Message,
            Exception = exception.GetType().FullName,
            StackTrace = exception.StackTrace,
            Source = exception.Source,
            CustomData = customData,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await LogAsync(entry, cancellationToken);
    }

    public async Task LogWarningAsync(string message, Dictionary<string, object>? customData = null, CancellationToken cancellationToken = default)
    {
        var entry = new LogEntry
        {
            Level = LogLevel.Warning,
            Message = message,
            CustomData = customData,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await LogAsync(entry, cancellationToken);
    }

    public async Task LogInfoAsync(string message, Dictionary<string, object>? customData = null, CancellationToken cancellationToken = default)
    {
        var entry = new LogEntry
        {
            Level = LogLevel.Information,
            Message = message,
            CustomData = customData,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await LogAsync(entry, cancellationToken);
    }

    public Task<LogEntry?> GetLogAsync(long id, CancellationToken cancellationToken = default)
    {
        return _logStore.GetByIdAsync(id, cancellationToken);
    }

    public Task<PagedResult<LogEntry>> QueryLogsAsync(LogFilter filter, CancellationToken cancellationToken = default)
    {
        return _logStore.QueryAsync(filter, cancellationToken);
    }

    public async Task<TraceSummary?> GetTraceAsync(string traceId, CancellationToken cancellationToken = default)
    {
        if (_traceStore == null) return null;
        return await _traceStore.GetTraceSummaryAsync(traceId, cancellationToken);
    }

    public async Task<PagedResult<TraceSummary>> QueryTracesAsync(TraceFilter filter, CancellationToken cancellationToken = default)
    {
        if (_traceStore == null) return PagedResult<TraceSummary>.Empty(filter.Page, filter.PageSize);
        return await _traceStore.QueryAsync(filter, cancellationToken);
    }

    public Task<LogStatistics> GetLogStatisticsAsync(DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default)
    {
        return _logStore.GetStatisticsAsync(from, to, cancellationToken);
    }

    public async Task<TraceStatistics> GetTraceStatisticsAsync(DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default)
    {
        if (_traceStore == null) return new TraceStatistics();
        return await _traceStore.GetStatisticsAsync(from, to, cancellationToken);
    }

    public Task ResolveLogAsync(long id, string? resolvedBy = null, CancellationToken cancellationToken = default)
    {
        return _logStore.ResolveAsync(id, resolvedBy, cancellationToken);
    }

    public Task DeleteLogAsync(long id, CancellationToken cancellationToken = default)
    {
        return _logStore.DeleteAsync(id, cancellationToken);
    }

    public async Task<byte[]> ExportLogsAsync(LogFilter filter, string exporterName, CancellationToken cancellationToken = default)
    {
        var exporter = _exporters.FirstOrDefault(e => e.Name.Equals(exporterName, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Exporter '{exporterName}' not found", nameof(exporterName));

        // Get all matching logs (up to a reasonable limit)
        filter.PageSize = 10000;
        var result = await _logStore.QueryAsync(filter, cancellationToken);

        return await exporter.ExportLogsAsync(result.Items, cancellationToken);
    }

    public async Task<byte[]> ExportTracesAsync(TraceFilter filter, string exporterName, CancellationToken cancellationToken = default)
    {
        if (_traceStore == null)
            throw new InvalidOperationException("Trace store is not configured");

        var exporter = _exporters.FirstOrDefault(e => e.Name.Equals(exporterName, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Exporter '{exporterName}' not found", nameof(exporterName));

        filter.PageSize = 10000;
        var result = await _traceStore.QueryAsync(filter, cancellationToken);

        return await exporter.ExportTracesAsync(result.Items, cancellationToken);
    }
}
