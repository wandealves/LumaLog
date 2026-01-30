namespace LumaLog.Models;

/// <summary>
/// Represents the status of a trace span.
/// </summary>
public enum SpanStatus
{
    Unset = 0,
    Ok = 1,
    Error = 2
}
