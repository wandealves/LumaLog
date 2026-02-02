using LumaLog.Abstractions;
using LumaLog.Models;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Serilog.Events;

namespace LumaLog.Serilog;

/// <summary>
/// Serilog sink that writes log events to LumaLog.
/// </summary>
public class LumaLogSink : ILogEventSink
{
    private readonly IServiceProvider _serviceProvider;
    private readonly LogLevel _minimumLevel;

    public LumaLogSink(IServiceProvider serviceProvider, LogLevel minimumLevel = LogLevel.Information)
    {
        _serviceProvider = serviceProvider;
        _minimumLevel = minimumLevel;
    }

    public void Emit(LogEvent logEvent)
    {
        var level = ConvertLevel(logEvent.Level);
        if (level < _minimumLevel) return;

        var logStore = _serviceProvider.GetService<ILogStore>();
        if (logStore == null) return;

        var entry = new LogEntry
        {
            Level = level,
            Message = logEvent.RenderMessage(),
            CreatedAt = logEvent.Timestamp
        };

        // Extract exception info
        if (logEvent.Exception != null)
        {
            entry.Exception = logEvent.Exception.GetType().FullName;
            entry.StackTrace = GetFullStackTrace(logEvent.Exception);
            entry.Source = logEvent.Exception.Source;
        }

        // Extract properties
        var customData = new Dictionary<string, object>();

        foreach (var property in logEvent.Properties)
        {
            var value = GetPropertyValue(property.Value);

            switch (property.Key)
            {
                case "TraceId":
                    entry.TraceId = value?.ToString();
                    break;
                case "SpanId":
                    entry.SpanId = value?.ToString();
                    break;
                case "ParentSpanId":
                    entry.ParentSpanId = value?.ToString();
                    break;
                case "UserId":
                    entry.UserId = value?.ToString();
                    break;
                case "UserName":
                    entry.UserName = value?.ToString();
                    break;
                case "RequestPath":
                    entry.RequestPath = value?.ToString();
                    break;
                case "RequestMethod":
                    entry.RequestMethod = value?.ToString();
                    break;
                case "IpAddress":
                    entry.IpAddress = value?.ToString();
                    break;
                case "StatusCode":
                    if (int.TryParse(value?.ToString(), out var statusCode))
                        entry.StatusCode = statusCode;
                    break;
                case "MachineName":
                    entry.MachineName = value?.ToString();
                    break;
                case "Environment":
                    entry.Environment = value?.ToString();
                    break;
                case "SourceContext":
                    entry.Source = value?.ToString();
                    break;
                default:
                    if (value != null)
                        customData[property.Key] = value;
                    break;
            }
        }

        if (customData.Count > 0)
        {
            entry.CustomData = customData;
        }

        // Fire and forget - we don't want to block the logging thread
        _ = Task.Run(async () =>
        {
            try
            {
                await logStore.InsertAsync(entry);
            }
            catch
            {
                // Silently ignore errors
            }
        });
    }

    private static LogLevel ConvertLevel(LogEventLevel level)
    {
        return level switch
        {
            LogEventLevel.Verbose => LogLevel.Trace,
            LogEventLevel.Debug => LogLevel.Debug,
            LogEventLevel.Information => LogLevel.Information,
            LogEventLevel.Warning => LogLevel.Warning,
            LogEventLevel.Error => LogLevel.Error,
            LogEventLevel.Fatal => LogLevel.Critical,
            _ => LogLevel.Information
        };
    }

    private static object? GetPropertyValue(LogEventPropertyValue propertyValue)
    {
        return propertyValue switch
        {
            ScalarValue scalar => scalar.Value,
            SequenceValue sequence => sequence.Elements.Select(GetPropertyValue).ToList(),
            StructureValue structure => structure.Properties.ToDictionary(p => p.Name, p => GetPropertyValue(p.Value)),
            DictionaryValue dictionary => dictionary.Elements.ToDictionary(
                e => GetPropertyValue(e.Key)?.ToString() ?? "",
                e => GetPropertyValue(e.Value)),
            _ => propertyValue.ToString()
        };
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
}
