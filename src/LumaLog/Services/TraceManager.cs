using LumaLog.Abstractions;
using LumaLog.Models;
using Microsoft.Extensions.Options;
using LumaLog.Configuration;

namespace LumaLog.Services;

/// <summary>
/// Manages trace context and span creation.
/// </summary>
public class TraceManager : ITraceManager
{
    private readonly ITraceStore? _traceStore;
    private readonly LumaLogOptions _options;

    public TraceManager(IOptions<LumaLogOptions> options, ITraceStore? traceStore = null)
    {
        _options = options.Value;
        _traceStore = traceStore;
    }

    public string? CurrentTraceId => TraceContext.Current?.TraceId;
    public string? CurrentSpanId => TraceContext.Current?.SpanId;
    public string? CurrentParentSpanId => TraceContext.Current?.ParentSpanId;

    public ISpan StartTrace(string name, string? serviceName = null)
    {
        var context = TraceContext.CreateNew(serviceName ?? _options.ApplicationName);
        TraceContext.Current = context;

        return new Span(this, context.TraceId, context.SpanId, null, name, context.ServiceName, _traceStore);
    }

    public ISpan StartSpan(string name)
    {
        var parentContext = TraceContext.Current;
        if (parentContext == null)
        {
            return StartTrace(name);
        }

        var childContext = parentContext.CreateChild();
        TraceContext.Current = childContext;

        return new Span(this, childContext.TraceId, childContext.SpanId, childContext.ParentSpanId, name, childContext.ServiceName, _traceStore);
    }

    public void SetContext(string traceId, string? spanId = null, string? parentSpanId = null)
    {
        TraceContext.Current = TraceContext.FromExternal(traceId, spanId, parentSpanId, _options.ApplicationName);
    }

    public void ClearContext()
    {
        TraceContext.Current = null;
    }

    public TraceContext GetCurrentContext()
    {
        return TraceContext.Current ?? TraceContext.CreateNew(_options.ApplicationName);
    }

    internal void RestoreParentContext(string? parentSpanId)
    {
        var current = TraceContext.Current;
        if (current != null && parentSpanId != null)
        {
            current.SpanId = parentSpanId;
            current.ParentSpanId = null;
        }
    }
}

/// <summary>
/// Represents an active span.
/// </summary>
internal class Span : ISpan
{
    private readonly TraceManager _manager;
    private readonly ITraceStore? _traceStore;
    private readonly TraceEntry _entry;
    private readonly string? _previousSpanId;
    private bool _completed;

    public Span(TraceManager manager, string traceId, string spanId, string? parentSpanId, string name, string? serviceName, ITraceStore? traceStore)
    {
        _manager = manager;
        _traceStore = traceStore;
        _previousSpanId = parentSpanId;

        _entry = new TraceEntry
        {
            TraceId = traceId,
            SpanId = spanId,
            ParentSpanId = parentSpanId,
            Name = name,
            ServiceName = serviceName,
            StartTime = DateTimeOffset.UtcNow,
            Status = SpanStatus.Unset,
            Tags = new Dictionary<string, string>(),
            Events = new List<SpanEvent>()
        };

        // Start persisting the span
        _ = PersistSpanAsync();
    }

    public string TraceId => _entry.TraceId;
    public string SpanId => _entry.SpanId;
    public string? ParentSpanId => _entry.ParentSpanId;
    public string Name => _entry.Name;
    public DateTimeOffset StartTime => _entry.StartTime;

    public ISpan SetTag(string key, string value)
    {
        _entry.Tags ??= new Dictionary<string, string>();
        _entry.Tags[key] = value;
        return this;
    }

    public ISpan AddEvent(string name, Dictionary<string, string>? attributes = null)
    {
        _entry.Events ??= new List<SpanEvent>();
        _entry.Events.Add(new SpanEvent
        {
            Name = name,
            Timestamp = DateTimeOffset.UtcNow,
            Attributes = attributes
        });
        return this;
    }

    public ISpan SetStatus(SpanStatus status, string? message = null)
    {
        _entry.Status = status;
        _entry.StatusMessage = message;
        return this;
    }

    public ISpan RecordException(Exception exception)
    {
        SetStatus(SpanStatus.Error, exception.Message);
        AddEvent("exception", new Dictionary<string, string>
        {
            ["exception.type"] = exception.GetType().FullName ?? exception.GetType().Name,
            ["exception.message"] = exception.Message,
            ["exception.stacktrace"] = exception.StackTrace ?? string.Empty
        });
        return this;
    }

    public void Complete()
    {
        if (_completed) return;
        _completed = true;

        _entry.EndTime = DateTimeOffset.UtcNow;
        _entry.DurationMs = (long)(_entry.EndTime.Value - _entry.StartTime).TotalMilliseconds;

        if (_entry.Status == SpanStatus.Unset)
        {
            _entry.Status = SpanStatus.Ok;
        }

        // Persist the completed span
        _ = CompleteSpanAsync();

        // Restore parent context
        _manager.RestoreParentContext(_previousSpanId);
    }

    public void Dispose()
    {
        Complete();
    }

    private async Task PersistSpanAsync()
    {
        if (_traceStore != null)
        {
            try
            {
                await _traceStore.InsertAsync(_entry);
            }
            catch
            {
                // Silently fail - we don't want tracing to break the application
            }
        }
    }

    private async Task CompleteSpanAsync()
    {
        if (_traceStore != null && _entry.EndTime.HasValue)
        {
            try
            {
                await _traceStore.CompleteSpanAsync(
                    _entry.SpanId,
                    _entry.EndTime.Value,
                    _entry.Status,
                    _entry.StatusMessage);
            }
            catch
            {
                // Silently fail
            }
        }
    }
}
