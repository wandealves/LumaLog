using System.Text.Json;
using LumaLog.Abstractions;
using LumaLog.Models;

namespace LumaLog.Services.Exporters;

/// <summary>
/// Exports logs and traces to JSON format.
/// </summary>
public class JsonExporter : IExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string Name => "json";
    public string ContentType => "application/json";
    public string FileExtension => ".json";

    public Task<byte[]> ExportLogsAsync(IEnumerable<LogEntry> entries, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(entries, JsonOptions);
        return Task.FromResult(json);
    }

    public Task<byte[]> ExportTracesAsync(IEnumerable<TraceSummary> traces, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(traces, JsonOptions);
        return Task.FromResult(json);
    }

    public async Task ExportLogsToStreamAsync(IEnumerable<LogEntry> entries, Stream stream, CancellationToken cancellationToken = default)
    {
        await JsonSerializer.SerializeAsync(stream, entries, JsonOptions, cancellationToken);
    }

    public async Task ExportTracesToStreamAsync(IEnumerable<TraceSummary> traces, Stream stream, CancellationToken cancellationToken = default)
    {
        await JsonSerializer.SerializeAsync(stream, traces, JsonOptions, cancellationToken);
    }
}
