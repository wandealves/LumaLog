using System.Diagnostics;
using LumaLog.Abstractions;
using LumaLog.Configuration;
using LumaLog.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace LumaLog.AspNetCore;

/// <summary>
/// Middleware for capturing errors and managing distributed traces.
/// </summary>
public class LumaLogMiddleware
{
    private readonly RequestDelegate _next;
    private readonly LumaLogOptions _options;

    public LumaLogMiddleware(RequestDelegate next, IOptions<LumaLogOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context, ITraceManager traceManager, ILumaLogService lumaLogService)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        // Check if path should be ignored
        if (ShouldIgnore(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Extract or create trace context
        var traceId = context.Request.Headers[_options.TraceIdHeaderName].FirstOrDefault();
        var parentSpanId = context.Request.Headers[_options.ParentSpanIdHeaderName].FirstOrDefault();

        ISpan? span = null;

        if (_options.TracingEnabled)
        {
            if (!string.IsNullOrEmpty(traceId))
            {
                traceManager.SetContext(traceId, null, parentSpanId);
                span = traceManager.StartSpan($"{context.Request.Method} {context.Request.Path}");
            }
            else
            {
                span = traceManager.StartTrace($"{context.Request.Method} {context.Request.Path}", _options.ApplicationName);
            }

            // Add trace headers to response
            context.Response.Headers[_options.TraceIdHeaderName] = span.TraceId;
            context.Response.Headers[_options.SpanIdHeaderName] = span.SpanId;

            // Set tags
            span.SetTag("http.method", context.Request.Method);
            span.SetTag("http.url", context.Request.Path.ToString());
            span.SetTag("http.scheme", context.Request.Scheme);

            if (context.Request.Host.HasValue)
            {
                span.SetTag("http.host", context.Request.Host.Value);
            }
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);

            stopwatch.Stop();

            if (span != null)
            {
                span.SetTag("http.status_code", context.Response.StatusCode.ToString());

                if (context.Response.StatusCode >= 400)
                {
                    span.SetStatus(SpanStatus.Error, $"HTTP {context.Response.StatusCode}");
                }
                else
                {
                    span.SetStatus(SpanStatus.Ok);
                }
            }

            // Log error responses
            if (context.Response.StatusCode >= 400)
            {
                await LogRequestAsync(context, lumaLogService, traceManager, stopwatch.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            span?.RecordException(ex);
            span?.SetStatus(SpanStatus.Error, ex.Message);

            await LogExceptionAsync(context, ex, lumaLogService, traceManager, stopwatch.ElapsedMilliseconds);

            throw;
        }
        finally
        {
            span?.Complete();
        }
    }

    private bool ShouldIgnore(PathString path)
    {
        if (_options.IgnorePaths == null || _options.IgnorePaths.Count == 0)
            return false;

        var pathValue = path.Value?.ToLowerInvariant() ?? "";

        return _options.IgnorePaths.Any(p =>
            pathValue.StartsWith(p.ToLowerInvariant()) ||
            pathValue.Equals(p.ToLowerInvariant()));
    }

    private async Task LogRequestAsync(HttpContext context, ILumaLogService lumaLogService, ITraceManager traceManager, long elapsedMs)
    {
        var entry = CreateLogEntry(context, traceManager, elapsedMs);
        entry.Level = context.Response.StatusCode >= 500 ? LogLevel.Error : LogLevel.Warning;
        entry.Message = $"HTTP {context.Response.StatusCode} - {context.Request.Method} {context.Request.Path}";

        await lumaLogService.LogAsync(entry);
    }

    private async Task LogExceptionAsync(HttpContext context, Exception ex, ILumaLogService lumaLogService, ITraceManager traceManager, long elapsedMs)
    {
        var entry = CreateLogEntry(context, traceManager, elapsedMs);
        entry.Level = LogLevel.Error;
        entry.Message = ex.Message;
        entry.Exception = ex.GetType().FullName;
        entry.StackTrace = GetFullStackTrace(ex);
        entry.Source = ex.Source;

        await lumaLogService.LogAsync(entry);
    }

    private static string GetFullStackTrace(Exception ex)
    {
        var sb = new System.Text.StringBuilder();

        var current = ex;
        var depth = 0;

        while (current != null)
        {
            if (depth > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"--- Inner Exception {depth} ---");
            }

            sb.AppendLine($"{current.GetType().FullName}: {current.Message}");

            if (!string.IsNullOrEmpty(current.StackTrace))
            {
                sb.AppendLine(current.StackTrace);
            }

            current = current.InnerException;
            depth++;
        }

        return sb.ToString();
    }

    private LogEntry CreateLogEntry(HttpContext context, ITraceManager traceManager, long elapsedMs)
    {
        var entry = new LogEntry
        {
            TraceId = traceManager.CurrentTraceId,
            SpanId = traceManager.CurrentSpanId,
            ParentSpanId = traceManager.CurrentParentSpanId,
            RequestPath = context.Request.Path.ToString(),
            RequestMethod = context.Request.Method,
            StatusCode = context.Response.StatusCode,
            IpAddress = GetClientIpAddress(context),
            CreatedAt = DateTimeOffset.UtcNow
        };

        // User info
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            entry.UserId = context.User.FindFirst("sub")?.Value ?? context.User.FindFirst("id")?.Value;
            entry.UserName = context.User.Identity.Name;
        }

        // Capture headers
        var customData = new Dictionary<string, object>();

        foreach (var headerName in _options.CaptureHeaders)
        {
            if (context.Request.Headers.TryGetValue(headerName, out var values) && values.Count > 0)
            {
                customData[$"header_{headerName}"] = values.ToString();
            }
        }

        customData["elapsed_ms"] = elapsedMs;

        if (customData.Count > 0)
        {
            entry.CustomData = customData;
        }

        return entry;
    }

    private static string? GetClientIpAddress(HttpContext context)
    {
        // Check X-Forwarded-For header first
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        // Check X-Real-IP header
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fall back to remote IP address
        return context.Connection.RemoteIpAddress?.ToString();
    }
}
