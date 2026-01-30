using LumaLog.Abstractions;
using LumaLog.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Serilog.Events;

namespace LumaLog.Serilog.Enrichers;

/// <summary>
/// Enriches log events with all LumaLog context (trace ID, span ID, user ID, etc.).
/// </summary>
public class LumaLogContextEnricher : ILogEventEnricher
{
    private readonly IServiceProvider _serviceProvider;

    public LumaLogContextEnricher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var traceManager = _serviceProvider.GetService<ITraceManager>();
        var httpContextAccessor = _serviceProvider.GetService<IHttpContextAccessor>();

        // Add trace context
        if (traceManager != null)
        {
            var traceId = traceManager.CurrentTraceId;
            if (!string.IsNullOrEmpty(traceId))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", traceId));
            }

            var spanId = traceManager.CurrentSpanId;
            if (!string.IsNullOrEmpty(spanId))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SpanId", spanId));
            }

            var parentSpanId = traceManager.CurrentParentSpanId;
            if (!string.IsNullOrEmpty(parentSpanId))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ParentSpanId", parentSpanId));
            }
        }

        // Add user context
        var httpContext = httpContextAccessor?.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var userId = httpContext.User.FindFirst("sub")?.Value
                ?? httpContext.User.FindFirst("id")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserId", userId));
            }

            var userName = httpContext.User.Identity.Name;
            if (!string.IsNullOrEmpty(userName))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserName", userName));
            }
        }

        // Add request context
        if (httpContext != null)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("RequestPath", httpContext.Request.Path.ToString()));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("RequestMethod", httpContext.Request.Method));

            var ipAddress = GetClientIpAddress(httpContext);
            if (!string.IsNullOrEmpty(ipAddress))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("IpAddress", ipAddress));
            }
        }
    }

    private static string? GetClientIpAddress(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }
}
