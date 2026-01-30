using LumaLog.Abstractions;
using Serilog.Core;
using Serilog.Events;

namespace LumaLog.Serilog.Enrichers;

/// <summary>
/// Enriches log events with the current trace ID.
/// </summary>
public class TraceIdEnricher : ILogEventEnricher
{
    private readonly ITraceManager _traceManager;

    public TraceIdEnricher(ITraceManager traceManager)
    {
        _traceManager = traceManager;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var traceId = _traceManager.CurrentTraceId;
        if (!string.IsNullOrEmpty(traceId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", traceId));
        }
    }
}
