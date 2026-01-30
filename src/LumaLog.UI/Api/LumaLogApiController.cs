using LumaLog.Abstractions;
using LumaLog.Configuration;
using LumaLog.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LumaLog.UI.Api;

/// <summary>
/// API endpoints for the LumaLog dashboard.
/// </summary>
[ApiController]
[Route("lumalog/api")]
public class LumaLogApiController : ControllerBase
{
    private readonly ILumaLogService _lumaLogService;
    private readonly LumaLogOptions _options;

    public LumaLogApiController(ILumaLogService lumaLogService, IOptions<LumaLogOptions> options)
    {
        _lumaLogService = lumaLogService;
        _options = options.Value;
    }

    [HttpGet("logs")]
    public async Task<ActionResult<PagedResult<LogEntry>>> GetLogs([FromQuery] LogFilter filter)
    {
        var result = await _lumaLogService.QueryLogsAsync(filter);
        return Ok(result);
    }

    [HttpGet("logs/{id}")]
    public async Task<ActionResult<LogEntry>> GetLog(long id)
    {
        var entry = await _lumaLogService.GetLogAsync(id);
        if (entry == null) return NotFound();
        return Ok(entry);
    }

    [HttpPost("logs/{id}/resolve")]
    public async Task<ActionResult> ResolveLog(long id, [FromQuery] string? resolvedBy = null)
    {
        await _lumaLogService.ResolveLogAsync(id, resolvedBy ?? User.Identity?.Name);
        return Ok();
    }

    [HttpPost("logs/{id}/unresolve")]
    public async Task<ActionResult> UnresolveLog(long id)
    {
        var entry = await _lumaLogService.GetLogAsync(id);
        if (entry == null) return NotFound();

        entry.IsResolved = false;
        entry.ResolvedAt = null;
        entry.ResolvedBy = null;

        return Ok();
    }

    [HttpDelete("logs/{id}")]
    public async Task<ActionResult> DeleteLog(long id)
    {
        await _lumaLogService.DeleteLogAsync(id);
        return Ok();
    }

    [HttpGet("traces")]
    public async Task<ActionResult<PagedResult<TraceSummary>>> GetTraces([FromQuery] TraceFilter filter)
    {
        var result = await _lumaLogService.QueryTracesAsync(filter);
        return Ok(result);
    }

    [HttpGet("traces/{traceId}")]
    public async Task<ActionResult<TraceSummary>> GetTrace(string traceId)
    {
        var trace = await _lumaLogService.GetTraceAsync(traceId);
        if (trace == null) return NotFound();
        return Ok(trace);
    }

    [HttpGet("statistics/logs")]
    public async Task<ActionResult<LogStatistics>> GetLogStatistics(
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null)
    {
        var stats = await _lumaLogService.GetLogStatisticsAsync(from, to);
        return Ok(stats);
    }

    [HttpGet("statistics/traces")]
    public async Task<ActionResult<TraceStatistics>> GetTraceStatistics(
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null)
    {
        var stats = await _lumaLogService.GetTraceStatisticsAsync(from, to);
        return Ok(stats);
    }

    [HttpGet("export/logs")]
    public async Task<ActionResult> ExportLogs([FromQuery] LogFilter filter, [FromQuery] string format = "json")
    {
        var data = await _lumaLogService.ExportLogsAsync(filter, format);
        var contentType = format.ToLower() switch
        {
            "csv" => "text/csv",
            _ => "application/json"
        };
        var fileName = $"lumalog-export-{DateTime.UtcNow:yyyyMMddHHmmss}.{format}";

        return File(data, contentType, fileName);
    }

    [HttpGet("export/traces")]
    public async Task<ActionResult> ExportTraces([FromQuery] TraceFilter filter, [FromQuery] string format = "json")
    {
        var data = await _lumaLogService.ExportTracesAsync(filter, format);
        var contentType = format.ToLower() switch
        {
            "csv" => "text/csv",
            _ => "application/json"
        };
        var fileName = $"lumalog-traces-{DateTime.UtcNow:yyyyMMddHHmmss}.{format}";

        return File(data, contentType, fileName);
    }
}
