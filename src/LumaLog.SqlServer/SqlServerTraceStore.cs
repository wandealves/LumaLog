using System.Text;
using Dapper;
using LumaLog.Abstractions;
using LumaLog.Models;
using Microsoft.Data.SqlClient;

namespace LumaLog.SqlServer;

/// <summary>
/// SQL Server implementation of ITraceStore.
/// </summary>
public class SqlServerTraceStore : ITraceStore
{
    private readonly string _connectionString;
    private readonly ILogStore? _logStore;

    public SqlServerTraceStore(string connectionString, ILogStore? logStore = null)
    {
        _connectionString = connectionString;
        _logStore = logStore;
    }

    public async Task<long> InsertAsync(TraceEntry entry, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO LumaLog_Traces
                (TraceId, SpanId, ParentSpanId, Name, StartTime, EndTime, DurationMs,
                 Status, StatusMessage, Tags, Events, ServiceName, CreatedAt)
            VALUES
                (@TraceId, @SpanId, @ParentSpanId, @Name, @StartTime, @EndTime, @DurationMs,
                 @Status, @StatusMessage, @TagsJson, @EventsJson, @ServiceName, @CreatedAt);
            SELECT CAST(SCOPE_IDENTITY() as bigint);";

        await using var connection = new SqlConnection(_connectionString);
        return await connection.ExecuteScalarAsync<long>(sql, entry);
    }

    public async Task InsertBatchAsync(IEnumerable<TraceEntry> entries, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO LumaLog_Traces
                (TraceId, SpanId, ParentSpanId, Name, StartTime, EndTime, DurationMs,
                 Status, StatusMessage, Tags, Events, ServiceName, CreatedAt)
            VALUES
                (@TraceId, @SpanId, @ParentSpanId, @Name, @StartTime, @EndTime, @DurationMs,
                 @Status, @StatusMessage, @TagsJson, @EventsJson, @ServiceName, @CreatedAt);";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        using var transaction = connection.BeginTransaction();
        try
        {
            await connection.ExecuteAsync(sql, entries, transaction);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<TraceEntry?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT *, Tags as TagsJson, Events as EventsJson FROM LumaLog_Traces WHERE Id = @Id";

        await using var connection = new SqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<TraceEntry>(sql, new { Id = id });
    }

    public async Task<TraceEntry?> GetBySpanIdAsync(string spanId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT *, Tags as TagsJson, Events as EventsJson FROM LumaLog_Traces WHERE SpanId = @SpanId";

        await using var connection = new SqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<TraceEntry>(sql, new { SpanId = spanId });
    }

    public async Task<List<TraceEntry>> GetByTraceIdAsync(string traceId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT *, Tags as TagsJson, Events as EventsJson FROM LumaLog_Traces WHERE TraceId = @TraceId ORDER BY StartTime";

        await using var connection = new SqlConnection(_connectionString);
        var result = await connection.QueryAsync<TraceEntry>(sql, new { TraceId = traceId });
        return result.ToList();
    }

    public async Task<TraceSummary?> GetTraceSummaryAsync(string traceId, CancellationToken cancellationToken = default)
    {
        var spans = await GetByTraceIdAsync(traceId, cancellationToken);
        if (spans.Count == 0) return null;

        var rootSpan = spans.FirstOrDefault(s => s.ParentSpanId == null) ?? spans.First();

        var summary = new TraceSummary
        {
            TraceId = traceId,
            RootSpanName = rootSpan.Name,
            StartTime = spans.Min(s => s.StartTime),
            EndTime = spans.Where(s => s.EndTime.HasValue).Select(s => s.EndTime!.Value).DefaultIfEmpty().Max(),
            SpanCount = spans.Count,
            ErrorCount = spans.Count(s => s.Status == SpanStatus.Error),
            OverallStatus = spans.Any(s => s.Status == SpanStatus.Error) ? SpanStatus.Error : SpanStatus.Ok,
            ServiceName = rootSpan.ServiceName,
            Spans = spans
        };

        if (summary.EndTime.HasValue && summary.EndTime.Value != default)
        {
            summary.TotalDurationMs = (long)(summary.EndTime.Value - summary.StartTime).TotalMilliseconds;
        }

        if (_logStore != null)
        {
            summary.RelatedLogs = await _logStore.GetByTraceIdAsync(traceId, cancellationToken);
        }

        return summary;
    }

    public async Task<PagedResult<TraceSummary>> QueryAsync(TraceFilter filter, CancellationToken cancellationToken = default)
    {
        var whereClause = new StringBuilder("WHERE 1=1");
        var parameters = new DynamicParameters();

        if (!string.IsNullOrEmpty(filter.ServiceName))
        {
            whereClause.Append(" AND ServiceName = @ServiceName");
            parameters.Add("ServiceName", filter.ServiceName);
        }

        if (filter.FromDate.HasValue)
        {
            whereClause.Append(" AND StartTime >= @FromDate");
            parameters.Add("FromDate", filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            whereClause.Append(" AND StartTime <= @ToDate");
            parameters.Add("ToDate", filter.ToDate.Value);
        }

        // Get distinct trace IDs with root span info
        var sql = $@"
            WITH RootSpans AS (
                SELECT
                    TraceId,
                    Name,
                    ServiceName,
                    StartTime,
                    ROW_NUMBER() OVER (PARTITION BY TraceId ORDER BY StartTime) as rn
                FROM LumaLog_Traces
                {whereClause}
            )
            SELECT TraceId, Name, ServiceName, StartTime
            FROM RootSpans
            WHERE rn = 1
            ORDER BY StartTime {(filter.SortDescending ? "DESC" : "ASC")}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var countSql = $@"
            SELECT COUNT(DISTINCT TraceId) FROM LumaLog_Traces {whereClause}";

        parameters.Add("Offset", (filter.Page - 1) * filter.PageSize);
        parameters.Add("PageSize", filter.PageSize);

        await using var connection = new SqlConnection(_connectionString);
        var total = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var traceInfos = await connection.QueryAsync<(string TraceId, string Name, string ServiceName, DateTimeOffset StartTime)>(sql, parameters);

        var summaries = new List<TraceSummary>();

        foreach (var info in traceInfos)
        {
            var summary = await GetTraceSummaryAsync(info.TraceId, cancellationToken);
            if (summary != null)
            {
                // Apply additional filters
                if (filter.Status.HasValue && summary.OverallStatus != filter.Status.Value)
                    continue;

                if (filter.MinDurationMs.HasValue && summary.TotalDurationMs < filter.MinDurationMs.Value)
                    continue;

                if (filter.MaxDurationMs.HasValue && summary.TotalDurationMs > filter.MaxDurationMs.Value)
                    continue;

                if (filter.HasErrors.HasValue)
                {
                    if (filter.HasErrors.Value && summary.ErrorCount == 0) continue;
                    if (!filter.HasErrors.Value && summary.ErrorCount > 0) continue;
                }

                if (!string.IsNullOrEmpty(filter.SearchTerm) &&
                    (summary.RootSpanName?.Contains(filter.SearchTerm, StringComparison.OrdinalIgnoreCase) != true))
                    continue;

                summaries.Add(summary);
            }
        }

        return PagedResult<TraceSummary>.Create(summaries, filter.Page, filter.PageSize, total);
    }

    public async Task CompleteSpanAsync(string spanId, DateTimeOffset endTime, SpanStatus status, string? statusMessage = null, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE LumaLog_Traces
            SET EndTime = @EndTime,
                DurationMs = DATEDIFF(MILLISECOND, StartTime, @EndTime),
                Status = @Status,
                StatusMessage = @StatusMessage
            WHERE SpanId = @SpanId";

        await using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new { SpanId = spanId, EndTime = endTime, Status = (int)status, StatusMessage = statusMessage });
    }

    public async Task AddSpanEventAsync(string spanId, SpanEvent spanEvent, CancellationToken cancellationToken = default)
    {
        var entry = await GetBySpanIdAsync(spanId, cancellationToken);
        if (entry != null)
        {
            entry.Events ??= new List<SpanEvent>();
            entry.Events.Add(spanEvent);

            const string sql = "UPDATE LumaLog_Traces SET Events = @EventsJson WHERE SpanId = @SpanId";

            await using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, new { SpanId = spanId, entry.EventsJson });
        }
    }

    public async Task<int> DeleteOlderThanAsync(DateTimeOffset date, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM LumaLog_Traces WHERE CreatedAt < @Date";

        await using var connection = new SqlConnection(_connectionString);
        return await connection.ExecuteAsync(sql, new { Date = date });
    }

    public async Task<TraceStatistics> GetStatisticsAsync(DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default)
    {
        var whereClause = new StringBuilder("WHERE 1=1");
        var parameters = new DynamicParameters();

        if (from.HasValue)
        {
            whereClause.Append(" AND StartTime >= @From");
            parameters.Add("From", from.Value);
        }

        if (to.HasValue)
        {
            whereClause.Append(" AND StartTime <= @To");
            parameters.Add("To", to.Value);
        }

        var sql = $@"
            SELECT
                COUNT(DISTINCT TraceId) as TotalTraces,
                COUNT(*) as TotalSpans,
                COUNT(DISTINCT CASE WHEN Status = 2 THEN TraceId ELSE NULL END) as ErrorTraces,
                AVG(CAST(DurationMs as FLOAT)) as AverageDurationMs,
                MAX(DurationMs) as MaxDurationMs,
                MIN(DurationMs) as MinDurationMs
            FROM LumaLog_Traces {whereClause}";

        await using var connection = new SqlConnection(_connectionString);
        return await connection.QueryFirstAsync<TraceStatistics>(sql, parameters);
    }

    public async Task EnsureTablesExistAsync(CancellationToken cancellationToken = default)
    {
        var createTablesSql = GetEmbeddedResource("CreateTables.sql");
        var indexesSql = GetEmbeddedResource("Indexes.sql");

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(createTablesSql);
        await connection.ExecuteAsync(indexesSql);
    }

    private static string GetEmbeddedResource(string resourceName)
    {
        var assembly = typeof(SqlServerTraceStore).Assembly;
        var fullName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(resourceName))
            ?? throw new InvalidOperationException($"Resource {resourceName} not found");

        using var stream = assembly.GetManifestResourceStream(fullName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
