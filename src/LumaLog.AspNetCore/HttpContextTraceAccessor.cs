using LumaLog.Abstractions;
using LumaLog.Configuration;
using LumaLog.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace LumaLog.AspNetCore;

/// <summary>
/// Provides access to trace context from HttpContext.
/// </summary>
public class HttpContextTraceAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITraceManager _traceManager;
    private readonly LumaLogOptions _options;

    public HttpContextTraceAccessor(
        IHttpContextAccessor httpContextAccessor,
        ITraceManager traceManager,
        IOptions<LumaLogOptions> options)
    {
        _httpContextAccessor = httpContextAccessor;
        _traceManager = traceManager;
        _options = options.Value;
    }

    /// <summary>
    /// Gets the current trace ID from HttpContext or trace manager.
    /// </summary>
    public string? GetTraceId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var traceId = httpContext.Request.Headers[_options.TraceIdHeaderName].FirstOrDefault();
            if (!string.IsNullOrEmpty(traceId))
                return traceId;
        }

        return _traceManager.CurrentTraceId;
    }

    /// <summary>
    /// Gets the current span ID from trace manager.
    /// </summary>
    public string? GetSpanId()
    {
        return _traceManager.CurrentSpanId;
    }

    /// <summary>
    /// Gets the current correlation ID (trace ID alias).
    /// </summary>
    public string? GetCorrelationId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var correlationId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                ?? httpContext.Request.Headers["X-Request-Id"].FirstOrDefault();

            if (!string.IsNullOrEmpty(correlationId))
                return correlationId;
        }

        return GetTraceId();
    }

    /// <summary>
    /// Gets the current trace context.
    /// </summary>
    public TraceContext GetContext()
    {
        return _traceManager.GetCurrentContext();
    }
}
