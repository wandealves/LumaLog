using LumaLog.AspNetCore;
using Serilog.Core;
using Serilog.Events;

namespace LumaLog.Serilog.Enrichers;

/// <summary>
/// Enriches log events with the correlation ID from HTTP headers.
/// </summary>
public class CorrelationIdEnricher : ILogEventEnricher
{
    private readonly HttpContextTraceAccessor _traceAccessor;

    public CorrelationIdEnricher(HttpContextTraceAccessor traceAccessor)
    {
        _traceAccessor = traceAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var correlationId = _traceAccessor.GetCorrelationId();
        if (!string.IsNullOrEmpty(correlationId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("CorrelationId", correlationId));
        }
    }
}
