using LumaLog.Models;
using LumaLog.Services;

namespace LumaLog.Abstractions;

/// <summary>
/// Contract for managing the current trace context and creating spans.
/// </summary>
public interface ITraceManager
{
    /// <summary>
    /// Gets the current trace ID, if any.
    /// </summary>
    string? CurrentTraceId { get; }

    /// <summary>
    /// Gets the current span ID, if any.
    /// </summary>
    string? CurrentSpanId { get; }

    /// <summary>
    /// Gets the current parent span ID, if any.
    /// </summary>
    string? CurrentParentSpanId { get; }

    /// <summary>
    /// Starts a new trace and returns the span.
    /// </summary>
    ISpan StartTrace(string name, string? serviceName = null);

    /// <summary>
    /// Starts a new span within the current trace.
    /// </summary>
    ISpan StartSpan(string name);

    /// <summary>
    /// Sets the current trace context from external values (e.g., from headers).
    /// </summary>
    void SetContext(string traceId, string? spanId = null, string? parentSpanId = null);

    /// <summary>
    /// Clears the current trace context.
    /// </summary>
    void ClearContext();

    /// <summary>
    /// Gets the current trace context for propagation.
    /// </summary>
    TraceContext GetCurrentContext();
}

/// <summary>
/// Represents an active span that can be completed.
/// </summary>
public interface ISpan : IDisposable
{
    /// <summary>
    /// Gets the trace ID.
    /// </summary>
    string TraceId { get; }

    /// <summary>
    /// Gets the span ID.
    /// </summary>
    string SpanId { get; }

    /// <summary>
    /// Gets the parent span ID, if any.
    /// </summary>
    string? ParentSpanId { get; }

    /// <summary>
    /// Gets the span name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the span start time.
    /// </summary>
    DateTimeOffset StartTime { get; }

    /// <summary>
    /// Sets a tag on the span.
    /// </summary>
    ISpan SetTag(string key, string value);

    /// <summary>
    /// Adds an event to the span.
    /// </summary>
    ISpan AddEvent(string name, Dictionary<string, string>? attributes = null);

    /// <summary>
    /// Sets the span status.
    /// </summary>
    ISpan SetStatus(SpanStatus status, string? message = null);

    /// <summary>
    /// Records an exception on the span.
    /// </summary>
    ISpan RecordException(Exception exception);

    /// <summary>
    /// Completes the span.
    /// </summary>
    void Complete();
}
