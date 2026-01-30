using LumaLog.Models;

namespace LumaLog.Abstractions;

/// <summary>
/// Contract for exporting log data to various formats.
/// </summary>
public interface IExporter
{
    /// <summary>
    /// Gets the unique name of this exporter.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the content type of the exported data.
    /// </summary>
    string ContentType { get; }

    /// <summary>
    /// Gets the file extension for the exported data.
    /// </summary>
    string FileExtension { get; }

    /// <summary>
    /// Exports log entries to a byte array.
    /// </summary>
    Task<byte[]> ExportLogsAsync(IEnumerable<LogEntry> entries, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports trace summaries to a byte array.
    /// </summary>
    Task<byte[]> ExportTracesAsync(IEnumerable<TraceSummary> traces, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports log entries to a stream.
    /// </summary>
    Task ExportLogsToStreamAsync(IEnumerable<LogEntry> entries, Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports trace summaries to a stream.
    /// </summary>
    Task ExportTracesToStreamAsync(IEnumerable<TraceSummary> traces, Stream stream, CancellationToken cancellationToken = default);
}
