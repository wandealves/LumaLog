using System.Globalization;
using System.Text;
using LumaLog.Abstractions;
using LumaLog.Models;

namespace LumaLog.Services.Exporters;

/// <summary>
/// Exports logs and traces to CSV format.
/// </summary>
public class CsvExporter : IExporter
{
    public string Name => "csv";
    public string ContentType => "text/csv";
    public string FileExtension => ".csv";

    public Task<byte[]> ExportLogsAsync(IEnumerable<LogEntry> entries, CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("Id,Level,Message,Exception,Source,TraceId,SpanId,UserId,UserName,IpAddress,RequestPath,RequestMethod,StatusCode,MachineName,Environment,CreatedAt,IsResolved");

        // Rows
        foreach (var entry in entries)
        {
            sb.AppendLine(string.Join(",",
                entry.Id,
                entry.Level,
                EscapeCsv(entry.Message),
                EscapeCsv(entry.Exception),
                EscapeCsv(entry.Source),
                EscapeCsv(entry.TraceId),
                EscapeCsv(entry.SpanId),
                EscapeCsv(entry.UserId),
                EscapeCsv(entry.UserName),
                EscapeCsv(entry.IpAddress),
                EscapeCsv(entry.RequestPath),
                EscapeCsv(entry.RequestMethod),
                entry.StatusCode?.ToString() ?? "",
                EscapeCsv(entry.MachineName),
                EscapeCsv(entry.Environment),
                entry.CreatedAt.ToString("o", CultureInfo.InvariantCulture),
                entry.IsResolved
            ));
        }

        return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    public Task<byte[]> ExportTracesAsync(IEnumerable<TraceSummary> traces, CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("TraceId,RootSpanName,ServiceName,StartTime,EndTime,TotalDurationMs,SpanCount,ErrorCount,OverallStatus");

        // Rows
        foreach (var trace in traces)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(trace.TraceId),
                EscapeCsv(trace.RootSpanName),
                EscapeCsv(trace.ServiceName),
                trace.StartTime.ToString("o", CultureInfo.InvariantCulture),
                trace.EndTime?.ToString("o", CultureInfo.InvariantCulture) ?? "",
                trace.TotalDurationMs?.ToString() ?? "",
                trace.SpanCount,
                trace.ErrorCount,
                trace.OverallStatus
            ));
        }

        return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    public async Task ExportLogsToStreamAsync(IEnumerable<LogEntry> entries, Stream stream, CancellationToken cancellationToken = default)
    {
        var bytes = await ExportLogsAsync(entries, cancellationToken);
        await stream.WriteAsync(bytes, cancellationToken);
    }

    public async Task ExportTracesToStreamAsync(IEnumerable<TraceSummary> traces, Stream stream, CancellationToken cancellationToken = default)
    {
        var bytes = await ExportTracesAsync(traces, cancellationToken);
        await stream.WriteAsync(bytes, cancellationToken);
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
