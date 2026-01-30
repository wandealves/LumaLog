using LumaLog.Models;
using LumaLog.Services;

namespace LumaLog.Tests;

public class InMemoryLogStoreTests
{
    private readonly InMemoryLogStore _store;

    public InMemoryLogStoreTests()
    {
        _store = new InMemoryLogStore();
    }

    [Fact]
    public async Task InsertAsync_ReturnsNewId()
    {
        var entry = CreateLogEntry(LogLevel.Information, "Test message");

        var id = await _store.InsertAsync(entry);

        Assert.True(id > 0);
        Assert.Equal(id, entry.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsEntry()
    {
        var entry = CreateLogEntry(LogLevel.Error, "Error message");
        var id = await _store.InsertAsync(entry);

        var result = await _store.GetByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal("Error message", result.Message);
        Assert.Equal(LogLevel.Error, result.Level);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistent_ReturnsNull()
    {
        var result = await _store.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task QueryAsync_FiltersbyLevel()
    {
        await _store.InsertAsync(CreateLogEntry(LogLevel.Information, "Info"));
        await _store.InsertAsync(CreateLogEntry(LogLevel.Warning, "Warning"));
        await _store.InsertAsync(CreateLogEntry(LogLevel.Error, "Error"));

        var filter = new LogFilter { MinLevel = LogLevel.Warning };
        var result = await _store.QueryAsync(filter);

        Assert.Equal(2, result.TotalItems);
        Assert.All(result.Items, item => Assert.True(item.Level >= LogLevel.Warning));
    }

    [Fact]
    public async Task QueryAsync_FiltersBySearchTerm()
    {
        await _store.InsertAsync(CreateLogEntry(LogLevel.Information, "User logged in"));
        await _store.InsertAsync(CreateLogEntry(LogLevel.Information, "User logged out"));
        await _store.InsertAsync(CreateLogEntry(LogLevel.Information, "System started"));

        var filter = new LogFilter { SearchTerm = "logged" };
        var result = await _store.QueryAsync(filter);

        Assert.Equal(2, result.TotalItems);
    }

    [Fact]
    public async Task QueryAsync_FiltersByTraceId()
    {
        var entry1 = CreateLogEntry(LogLevel.Information, "Message 1");
        entry1.TraceId = "trace-123";
        await _store.InsertAsync(entry1);

        var entry2 = CreateLogEntry(LogLevel.Information, "Message 2");
        entry2.TraceId = "trace-456";
        await _store.InsertAsync(entry2);

        var filter = new LogFilter { TraceId = "trace-123" };
        var result = await _store.QueryAsync(filter);

        Assert.Single(result.Items);
        Assert.Equal("trace-123", result.Items[0].TraceId);
    }

    [Fact]
    public async Task QueryAsync_Paginates()
    {
        for (int i = 0; i < 25; i++)
        {
            await _store.InsertAsync(CreateLogEntry(LogLevel.Information, $"Message {i}"));
        }

        var filter = new LogFilter { Page = 1, PageSize = 10 };
        var result = await _store.QueryAsync(filter);

        Assert.Equal(10, result.Items.Count);
        Assert.Equal(25, result.TotalItems);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
    }

    [Fact]
    public async Task ResolveAsync_MarksAsResolved()
    {
        var entry = CreateLogEntry(LogLevel.Error, "Error");
        var id = await _store.InsertAsync(entry);

        await _store.ResolveAsync(id, "admin");

        var result = await _store.GetByIdAsync(id);
        Assert.True(result!.IsResolved);
        Assert.NotNull(result.ResolvedAt);
        Assert.Equal("admin", result.ResolvedBy);
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntry()
    {
        var entry = CreateLogEntry(LogLevel.Information, "To delete");
        var id = await _store.InsertAsync(entry);

        await _store.DeleteAsync(id);

        var result = await _store.GetByIdAsync(id);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteOlderThanAsync_RemovesOldEntries()
    {
        var oldEntry = CreateLogEntry(LogLevel.Information, "Old");
        oldEntry.CreatedAt = DateTimeOffset.UtcNow.AddDays(-10);
        await _store.InsertAsync(oldEntry);

        var newEntry = CreateLogEntry(LogLevel.Information, "New");
        await _store.InsertAsync(newEntry);

        var deleted = await _store.DeleteOlderThanAsync(DateTimeOffset.UtcNow.AddDays(-5));

        Assert.Equal(1, deleted);
    }

    [Fact]
    public async Task GetStatisticsAsync_ReturnsCorrectCounts()
    {
        await _store.InsertAsync(CreateLogEntry(LogLevel.Information, "Info 1"));
        await _store.InsertAsync(CreateLogEntry(LogLevel.Information, "Info 2"));
        await _store.InsertAsync(CreateLogEntry(LogLevel.Warning, "Warning"));
        await _store.InsertAsync(CreateLogEntry(LogLevel.Error, "Error"));
        await _store.InsertAsync(CreateLogEntry(LogLevel.Critical, "Critical"));

        var stats = await _store.GetStatisticsAsync();

        Assert.Equal(5, stats.TotalLogs);
        Assert.Equal(1, stats.TotalErrors);
        Assert.Equal(1, stats.TotalWarnings);
        Assert.Equal(1, stats.TotalCritical);
    }

    [Fact]
    public async Task GetByTraceIdAsync_ReturnsRelatedLogs()
    {
        var entry1 = CreateLogEntry(LogLevel.Information, "Message 1");
        entry1.TraceId = "trace-abc";
        await _store.InsertAsync(entry1);

        var entry2 = CreateLogEntry(LogLevel.Error, "Message 2");
        entry2.TraceId = "trace-abc";
        await _store.InsertAsync(entry2);

        var entry3 = CreateLogEntry(LogLevel.Information, "Message 3");
        entry3.TraceId = "trace-xyz";
        await _store.InsertAsync(entry3);

        var result = await _store.GetByTraceIdAsync("trace-abc");

        Assert.Equal(2, result.Count);
    }

    private static LogEntry CreateLogEntry(LogLevel level, string message)
    {
        return new LogEntry
        {
            Level = level,
            Message = message,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
