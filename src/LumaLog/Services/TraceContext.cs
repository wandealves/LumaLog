namespace LumaLog.Services;

/// <summary>
/// Represents the current trace context.
/// </summary>
public class TraceContext
{
    private static readonly AsyncLocal<TraceContext?> _current = new();

    /// <summary>
    /// Gets or sets the current trace context.
    /// </summary>
    public static TraceContext? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    /// <summary>
    /// Gets or sets the trace ID.
    /// </summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current span ID.
    /// </summary>
    public string SpanId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parent span ID.
    /// </summary>
    public string? ParentSpanId { get; set; }

    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Creates a new trace context with a new trace ID.
    /// </summary>
    public static TraceContext CreateNew(string? serviceName = null)
    {
        return new TraceContext
        {
            TraceId = GenerateId(),
            SpanId = GenerateId(),
            ParentSpanId = null,
            ServiceName = serviceName
        };
    }

    /// <summary>
    /// Creates a child context for a new span.
    /// </summary>
    public TraceContext CreateChild()
    {
        return new TraceContext
        {
            TraceId = TraceId,
            SpanId = GenerateId(),
            ParentSpanId = SpanId,
            ServiceName = ServiceName
        };
    }

    /// <summary>
    /// Creates a context from external values.
    /// </summary>
    public static TraceContext FromExternal(string traceId, string? spanId = null, string? parentSpanId = null, string? serviceName = null)
    {
        return new TraceContext
        {
            TraceId = traceId,
            SpanId = spanId ?? GenerateId(),
            ParentSpanId = parentSpanId,
            ServiceName = serviceName
        };
    }

    private static string GenerateId()
    {
        return Guid.NewGuid().ToString("N")[..16];
    }
}
