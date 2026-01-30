using LumaLog.Abstractions;
using LumaLog.Configuration;
using LumaLog.Models;
using LumaLog.Services;
using Microsoft.Extensions.Options;
using Moq;

namespace LumaLog.Tests;

public class LumaLogServiceTests
{
    private readonly InMemoryLogStore _logStore;
    private readonly InMemoryTraceStore _traceStore;
    private readonly TraceManager _traceManager;
    private readonly LumaLogService _service;

    public LumaLogServiceTests()
    {
        var options = Options.Create(new LumaLogOptions
        {
            Enabled = true,
            ApplicationName = "TestApp",
            Environment = "Test",
            MinimumLevel = LogLevel.Information
        });

        _logStore = new InMemoryLogStore();
        _traceStore = new InMemoryTraceStore(_logStore);
        _traceManager = new TraceManager(options, _traceStore);

        _service = new LumaLogService(
            _logStore,
            _traceManager,
            options,
            _traceStore);
    }

    [Fact]
    public async Task LogAsync_InsertsEntry()
    {
        var entry = new LogEntry
        {
            Level = LogLevel.Information,
            Message = "Test message"
        };

        await _service.LogAsync(entry);

        var result = await _service.QueryLogsAsync(new LogFilter());
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task LogAsync_EnrichesWithEnvironment()
    {
        var entry = new LogEntry
        {
            Level = LogLevel.Information,
            Message = "Test"
        };

        await _service.LogAsync(entry);

        var result = await _service.QueryLogsAsync(new LogFilter());
        Assert.Equal("Test", result.Items[0].Environment);
    }

    [Fact]
    public async Task LogAsync_EnrichesWithTraceContext()
    {
        using var span = _traceManager.StartTrace("test-operation");

        var entry = new LogEntry
        {
            Level = LogLevel.Information,
            Message = "Test"
        };

        await _service.LogAsync(entry);

        var result = await _service.QueryLogsAsync(new LogFilter());
        Assert.Equal(span.TraceId, result.Items[0].TraceId);
    }

    [Fact]
    public async Task LogAsync_BelowMinLevel_DoesNotInsert()
    {
        var entry = new LogEntry
        {
            Level = LogLevel.Debug,
            Message = "Debug message"
        };

        await _service.LogAsync(entry);

        var result = await _service.QueryLogsAsync(new LogFilter { MinLevel = LogLevel.Trace });
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task LogErrorAsync_CreatesErrorEntry()
    {
        var exception = new InvalidOperationException("Test error");

        await _service.LogErrorAsync(exception, "Something went wrong");

        var result = await _service.QueryLogsAsync(new LogFilter());
        Assert.Single(result.Items);
        Assert.Equal(LogLevel.Error, result.Items[0].Level);
        Assert.Equal("Something went wrong", result.Items[0].Message);
        Assert.Contains("InvalidOperationException", result.Items[0].Exception);
    }

    [Fact]
    public async Task LogWarningAsync_CreatesWarningEntry()
    {
        await _service.LogWarningAsync("Warning message");

        var result = await _service.QueryLogsAsync(new LogFilter());
        Assert.Single(result.Items);
        Assert.Equal(LogLevel.Warning, result.Items[0].Level);
    }

    [Fact]
    public async Task LogInfoAsync_CreatesInfoEntry()
    {
        await _service.LogInfoAsync("Info message", new Dictionary<string, object>
        {
            ["key"] = "value"
        });

        var result = await _service.QueryLogsAsync(new LogFilter());
        Assert.Single(result.Items);
        Assert.Equal(LogLevel.Information, result.Items[0].Level);
        Assert.NotNull(result.Items[0].CustomData);
    }

    [Fact]
    public async Task ResolveLogAsync_ResolvesEntry()
    {
        var entry = new LogEntry
        {
            Level = LogLevel.Error,
            Message = "Error to resolve"
        };
        await _service.LogAsync(entry);
        var log = (await _service.QueryLogsAsync(new LogFilter())).Items[0];

        await _service.ResolveLogAsync(log.Id, "admin");

        var resolved = await _service.GetLogAsync(log.Id);
        Assert.True(resolved!.IsResolved);
        Assert.Equal("admin", resolved.ResolvedBy);
    }

    [Fact]
    public async Task DeleteLogAsync_RemovesEntry()
    {
        var entry = new LogEntry
        {
            Level = LogLevel.Information,
            Message = "To delete"
        };
        await _service.LogAsync(entry);
        var log = (await _service.QueryLogsAsync(new LogFilter())).Items[0];

        await _service.DeleteLogAsync(log.Id);

        var result = await _service.GetLogAsync(log.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetLogStatisticsAsync_ReturnsStats()
    {
        await _service.LogInfoAsync("Info");
        await _service.LogWarningAsync("Warning");
        await _service.LogErrorAsync(new Exception("Error"));

        var stats = await _service.GetLogStatisticsAsync();

        Assert.Equal(3, stats.TotalLogs);
        Assert.Equal(1, stats.TotalErrors);
        Assert.Equal(1, stats.TotalWarnings);
    }

    [Fact]
    public async Task QueryLogsAsync_WithFilter_ReturnsFiltered()
    {
        await _service.LogInfoAsync("Message A");
        await _service.LogInfoAsync("Message B");
        await _service.LogErrorAsync(new Exception("Error C"));

        var filter = new LogFilter { SearchTerm = "Error" };
        var result = await _service.QueryLogsAsync(filter);

        Assert.Single(result.Items);
    }
}
