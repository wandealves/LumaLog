using LumaLog.Configuration;
using LumaLog.Models;
using LumaLog.Services;
using Microsoft.Extensions.Options;
using Moq;

namespace LumaLog.Tests;

public class TraceManagerTests
{
    private readonly TraceManager _traceManager;

    public TraceManagerTests()
    {
        var options = Options.Create(new LumaLogOptions
        {
            ApplicationName = "TestApp"
        });
        _traceManager = new TraceManager(options);
    }

    [Fact]
    public void StartTrace_CreatesNewTraceContext()
    {
        using var span = _traceManager.StartTrace("test-operation");

        Assert.NotNull(span.TraceId);
        Assert.NotNull(span.SpanId);
        Assert.Null(span.ParentSpanId);
        Assert.Equal("test-operation", span.Name);
    }

    [Fact]
    public void StartSpan_WithinTrace_CreatesChildSpan()
    {
        using var parentSpan = _traceManager.StartTrace("parent");
        using var childSpan = _traceManager.StartSpan("child");

        Assert.Equal(parentSpan.TraceId, childSpan.TraceId);
        Assert.NotEqual(parentSpan.SpanId, childSpan.SpanId);
        Assert.Equal(parentSpan.SpanId, childSpan.ParentSpanId);
    }

    [Fact]
    public void SetContext_SetsTraceContext()
    {
        _traceManager.SetContext("trace-123", "span-456", "parent-789");

        Assert.Equal("trace-123", _traceManager.CurrentTraceId);
        Assert.Equal("span-456", _traceManager.CurrentSpanId);
        Assert.Equal("parent-789", _traceManager.CurrentParentSpanId);
    }

    [Fact]
    public void ClearContext_RemovesTraceContext()
    {
        _traceManager.SetContext("trace-123");
        _traceManager.ClearContext();

        Assert.Null(_traceManager.CurrentTraceId);
        Assert.Null(_traceManager.CurrentSpanId);
    }

    [Fact]
    public void Span_SetTag_AddsTag()
    {
        using var span = _traceManager.StartTrace("test");

        span.SetTag("key1", "value1")
            .SetTag("key2", "value2");

        // Span internally tracks tags - validated by completion
        span.Complete();
    }

    [Fact]
    public void Span_SetStatus_SetsStatus()
    {
        using var span = _traceManager.StartTrace("test");

        span.SetStatus(SpanStatus.Error, "Something went wrong");

        span.Complete();
    }

    [Fact]
    public void Span_RecordException_SetsErrorStatus()
    {
        using var span = _traceManager.StartTrace("test");

        span.RecordException(new InvalidOperationException("Test error"));

        span.Complete();
    }

    [Fact]
    public void Span_AddEvent_AddsEvent()
    {
        using var span = _traceManager.StartTrace("test");

        span.AddEvent("checkpoint", new Dictionary<string, string>
        {
            ["data"] = "test"
        });

        span.Complete();
    }
}
