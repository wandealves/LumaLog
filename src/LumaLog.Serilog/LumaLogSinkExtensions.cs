using LumaLog.Models;
using Serilog;
using Serilog.Configuration;

namespace LumaLog.Serilog;

/// <summary>
/// Extension methods for configuring LumaLog Serilog sink.
/// </summary>
public static class LumaLogSinkExtensions
{
    /// <summary>
    /// Writes log events to LumaLog.
    /// </summary>
    public static LoggerConfiguration LumaLog(
        this LoggerSinkConfiguration sinkConfiguration,
        IServiceProvider serviceProvider,
        LogLevel minimumLevel = LogLevel.Information)
    {
        return sinkConfiguration.Sink(new LumaLogSink(serviceProvider, minimumLevel));
    }
}
