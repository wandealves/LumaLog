using LumaLog.Models;

namespace LumaLog.Configuration;

/// <summary>
/// Main configuration options for LumaLog.
/// </summary>
public class LumaLogOptions
{
    /// <summary>
    /// Gets or sets whether LumaLog is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether distributed tracing is enabled.
    /// </summary>
    public bool TracingEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum log level to capture.
    /// </summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Gets or sets the application name for identification.
    /// </summary>
    public string? ApplicationName { get; set; }

    /// <summary>
    /// Gets or sets the environment name (e.g., Development, Production).
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Gets or sets the dashboard path.
    /// </summary>
    public string DashboardPath { get; set; } = "/lumalog";

    /// <summary>
    /// Gets or sets whether authentication is required for the dashboard.
    /// </summary>
    public bool RequireAuthentication { get; set; } = false;

    /// <summary>
    /// Gets or sets the authorization predicate for dashboard access.
    /// </summary>
    public Func<object, bool>? AuthorizationPredicate { get; set; }

    /// <summary>
    /// Gets or sets the allowed roles for dashboard access.
    /// </summary>
    public List<string>? AllowedRoles { get; set; }

    /// <summary>
    /// Gets or sets the batch size for inserting logs.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the batch flush interval in milliseconds.
    /// </summary>
    public int BatchFlushIntervalMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets whether to capture request/response bodies.
    /// </summary>
    public bool CaptureRequestBody { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to capture response bodies.
    /// </summary>
    public bool CaptureResponseBody { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum request body size to capture.
    /// </summary>
    public int MaxRequestBodySize { get; set; } = 32768;

    /// <summary>
    /// Gets or sets paths to ignore from logging.
    /// </summary>
    public List<string> IgnorePaths { get; set; } = new()
    {
        "/health",
        "/healthz",
        "/ready",
        "/live",
        "/metrics",
        "/favicon.ico"
    };

    /// <summary>
    /// Gets or sets header names to capture.
    /// </summary>
    public List<string> CaptureHeaders { get; set; } = new()
    {
        "User-Agent",
        "Referer",
        "X-Forwarded-For",
        "X-Request-Id",
        "X-Correlation-Id"
    };

    /// <summary>
    /// Gets or sets header names to exclude from capture (for sensitive data).
    /// </summary>
    public List<string> ExcludeHeaders { get; set; } = new()
    {
        "Authorization",
        "Cookie",
        "X-Api-Key"
    };

    /// <summary>
    /// Gets or sets the retention period in days for logs.
    /// </summary>
    public int RetentionDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to enable automatic cleanup of old logs.
    /// </summary>
    public bool EnableAutoCleanup { get; set; } = true;

    /// <summary>
    /// Gets or sets the cleanup interval in hours.
    /// </summary>
    public int CleanupIntervalHours { get; set; } = 24;

    /// <summary>
    /// Gets or sets whether to capture unhandled exceptions.
    /// </summary>
    public bool CaptureUnhandledExceptions { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include machine name in logs.
    /// </summary>
    public bool IncludeMachineName { get; set; } = true;

    /// <summary>
    /// Gets or sets the trace header name for propagation.
    /// </summary>
    public string TraceIdHeaderName { get; set; } = "X-Trace-Id";

    /// <summary>
    /// Gets or sets the span header name for propagation.
    /// </summary>
    public string SpanIdHeaderName { get; set; } = "X-Span-Id";

    /// <summary>
    /// Gets or sets the parent span header name for propagation.
    /// </summary>
    public string ParentSpanIdHeaderName { get; set; } = "X-Parent-Span-Id";
}
