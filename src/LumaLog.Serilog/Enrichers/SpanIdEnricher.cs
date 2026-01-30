using LumaLog.Abstractions;
using Serilog.Core;
using Serilog.Events;

namespace LumaLog.Serilog.Enrichers;

/// <summary>
/// Enriches log events with the current span ID.
/// </summary>
public class SpanIdEnricher : ILogEventEnricher
{
    private readonly ITraceManager _traceManager;

    public SpanIdEnricher(ITraceManager traceManager)
    {
        _traceManager = traceManager;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var spanId = _traceManager.CurrentSpanId;
        if (!string.IsNullOrEmpty(spanId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SpanId", spanId));
        }

        var parentSpanId = _traceManager.CurrentParentSpanId;
        if (!string.IsNullOrEmpty(parentSpanId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ParentSpanId", parentSpanId));
        }
    }
}
