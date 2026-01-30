using System.Text;
using Dapper;
using LumaLog.Abstractions;
using LumaLog.Models;
using Microsoft.Data.SqlClient;

namespace LumaLog.SqlServer;

/// <summary>
/// SQL Server implementation of ILogStore.
/// </summary>
public class SqlServerLogStore : ILogStore
{
    private readonly string _connectionString;

    public SqlServerLogStore(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<long> InsertAsync(LogEntry entry, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO LumaLog_Entries
                (Level, Message, Exception, StackTrace, Source, TraceId, SpanId, ParentSpanId,
                 UserId, UserName, IpAddress, RequestPath, RequestMethod, StatusCode,
                 MachineName, Environment, CustomData, CreatedAt, IsResolved)
            VALUES
                (@Level, @Message, @Exception, @StackTrace, @Source, @TraceId, @SpanId, @ParentSpanId,
                 @UserId, @UserName, @IpAddress, @RequestPath, @RequestMethod, @StatusCode,
                 @MachineName, @Environment, @CustomDataJson, @CreatedAt, @IsResolved);
            SELECT CAST(SCOPE_IDENTITY() as bigint);";

        await using var connection = new SqlConnection(_connectionString);
        return await connection.ExecuteScalarAsync<long>(sql, entry);
    }

    public async Task InsertBatchAsync(IEnumerable<LogEntry> entries, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO LumaLog_Entries
                (Level, Message, Exception, StackTrace, Source, TraceId, SpanId, ParentSpanId,
                 UserId, UserName, IpAddress, RequestPath, RequestMethod, StatusCode,
                 MachineName, Environment, CustomData, CreatedAt, IsResolved)
            VALUES
                (@Level, @Message, @Exception, @StackTrace, @Source, @TraceId, @SpanId, @ParentSpanId,
                 @UserId, @UserName, @IpAddress, @RequestPath, @RequestMethod, @StatusCode,
                 @MachineName, @Environment, @CustomDataJson, @CreatedAt, @IsResolved);";

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

    public async Task<LogEntry?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT *, CustomData as CustomDataJson FROM LumaLog_Entries WHERE Id = @Id";

        await using var connection = new SqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<LogEntry>(sql, new { Id = id });
    }

    public async Task<List<LogEntry>> GetByTraceIdAsync(string traceId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT *, CustomData as CustomDataJson FROM LumaLog_Entries WHERE TraceId = @TraceId ORDER BY CreatedAt";

        await using var connection = new SqlConnection(_connectionString);
        var result = await connection.QueryAsync<LogEntry>(sql, new { TraceId = traceId });
        return result.ToList();
    }

    public async Task<PagedResult<LogEntry>> QueryAsync(LogFilter filter, CancellationToken cancellationToken = default)
    {
        var whereClause = new StringBuilder("WHERE 1=1");
        var parameters = new DynamicParameters();

        if (filter.MinLevel.HasValue)
        {
            whereClause.Append(" AND Level >= @MinLevel");
            parameters.Add("MinLevel", (int)filter.MinLevel.Value);
        }

        if (filter.MaxLevel.HasValue)
        {
            whereClause.Append(" AND Level <= @MaxLevel");
            parameters.Add("MaxLevel", (int)filter.MaxLevel.Value);
        }

        if (filter.Levels != null && filter.Levels.Count > 0)
        {
            whereClause.Append(" AND Level IN @Levels");
            parameters.Add("Levels", filter.Levels.Select(l => (int)l).ToArray());
        }

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            whereClause.Append(" AND Message LIKE @SearchTerm");
            parameters.Add("SearchTerm", $"%{filter.SearchTerm}%");
        }

        if (!string.IsNullOrEmpty(filter.TraceId))
        {
            whereClause.Append(" AND TraceId = @TraceId");
            parameters.Add("TraceId", filter.TraceId);
        }

        if (!string.IsNullOrEmpty(filter.UserId))
        {
            whereClause.Append(" AND UserId = @UserId");
            parameters.Add("UserId", filter.UserId);
        }

        if (!string.IsNullOrEmpty(filter.Source))
        {
            whereClause.Append(" AND Source LIKE @Source");
            parameters.Add("Source", $"%{filter.Source}%");
        }

        if (!string.IsNullOrEmpty(filter.RequestPath))
        {
            whereClause.Append(" AND RequestPath LIKE @RequestPath");
            parameters.Add("RequestPath", $"%{filter.RequestPath}%");
        }

        if (!string.IsNullOrEmpty(filter.RequestMethod))
        {
            whereClause.Append(" AND RequestMethod = @RequestMethod");
            parameters.Add("RequestMethod", filter.RequestMethod);
        }

        if (filter.StatusCode.HasValue)
        {
            whereClause.Append(" AND StatusCode = @StatusCode");
            parameters.Add("StatusCode", filter.StatusCode.Value);
        }

        if (!string.IsNullOrEmpty(filter.Environment))
        {
            whereClause.Append(" AND Environment = @Environment");
            parameters.Add("Environment", filter.Environment);
        }

        if (filter.FromDate.HasValue)
        {
            whereClause.Append(" AND CreatedAt >= @FromDate");
            parameters.Add("FromDate", filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            whereClause.Append(" AND CreatedAt <= @ToDate");
            parameters.Add("ToDate", filter.ToDate.Value);
        }

        if (filter.IsResolved.HasValue)
        {
            whereClause.Append(" AND IsResolved = @IsResolved");
            parameters.Add("IsResolved", filter.IsResolved.Value);
        }

        if (filter.HasException.HasValue)
        {
            if (filter.HasException.Value)
                whereClause.Append(" AND Exception IS NOT NULL");
            else
                whereClause.Append(" AND Exception IS NULL");
        }

        var orderBy = filter.SortDescending ? "DESC" : "ASC";
        var offset = (filter.Page - 1) * filter.PageSize;

        var countSql = $"SELECT COUNT(*) FROM LumaLog_Entries {whereClause}";
        var dataSql = $@"
            SELECT *, CustomData as CustomDataJson
            FROM LumaLog_Entries
            {whereClause}
            ORDER BY {filter.SortBy ?? "CreatedAt"} {orderBy}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", filter.PageSize);

        await using var connection = new SqlConnection(_connectionString);
        var total = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var items = (await connection.QueryAsync<LogEntry>(dataSql, parameters)).ToList();

        return PagedResult<LogEntry>.Create(items, filter.Page, filter.PageSize, total);
    }

    public async Task ResolveAsync(long id, string? resolvedBy = null, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE LumaLog_Entries
            SET IsResolved = 1, ResolvedAt = SYSDATETIMEOFFSET(), ResolvedBy = @ResolvedBy
            WHERE Id = @Id";

        await using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new { Id = id, ResolvedBy = resolvedBy });
    }

    public async Task UnresolveAsync(long id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE LumaLog_Entries
            SET IsResolved = 0, ResolvedAt = NULL, ResolvedBy = NULL
            WHERE Id = @Id";

        await using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM LumaLog_Entries WHERE Id = @Id";

        await using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<int> DeleteOlderThanAsync(DateTimeOffset date, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM LumaLog_Entries WHERE CreatedAt < @Date";

        await using var connection = new SqlConnection(_connectionString);
        return await connection.ExecuteAsync(sql, new { Date = date });
    }

    public async Task<LogStatistics> GetStatisticsAsync(DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default)
    {
        var whereClause = new StringBuilder("WHERE 1=1");
        var parameters = new DynamicParameters();

        if (from.HasValue)
        {
            whereClause.Append(" AND CreatedAt >= @From");
            parameters.Add("From", from.Value);
        }

        if (to.HasValue)
        {
            whereClause.Append(" AND CreatedAt <= @To");
            parameters.Add("To", to.Value);
        }

        var sql = $@"
            SELECT
                COUNT(*) as TotalLogs,
                SUM(CASE WHEN Level = 4 THEN 1 ELSE 0 END) as TotalErrors,
                SUM(CASE WHEN Level = 3 THEN 1 ELSE 0 END) as TotalWarnings,
                SUM(CASE WHEN Level = 5 THEN 1 ELSE 0 END) as TotalCritical,
                SUM(CASE WHEN Level >= 4 AND IsResolved = 0 THEN 1 ELSE 0 END) as UnresolvedErrors,
                MAX(CASE WHEN Level >= 4 THEN CreatedAt ELSE NULL END) as LastErrorTime
            FROM LumaLog_Entries {whereClause}";

        await using var connection = new SqlConnection(_connectionString);
        var stats = await connection.QueryFirstAsync<LogStatistics>(sql, parameters);

        // Get count by level
        var levelSql = $@"
            SELECT Level, COUNT(*) as Count
            FROM LumaLog_Entries {whereClause}
            GROUP BY Level";

        var levelCounts = await connection.QueryAsync<(int Level, long Count)>(levelSql, parameters);
        stats.CountByLevel = levelCounts.ToDictionary(x => (LogLevel)x.Level, x => x.Count);

        return stats;
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
        var assembly = typeof(SqlServerLogStore).Assembly;
        var fullName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(resourceName))
            ?? throw new InvalidOperationException($"Resource {resourceName} not found");

        using var stream = assembly.GetManifestResourceStream(fullName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
