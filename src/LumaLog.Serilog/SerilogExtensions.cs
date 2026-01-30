using LumaLog.Serilog.Enrichers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Configuration;

namespace LumaLog.Serilog;

/// <summary>
/// Extension methods for configuring Serilog with LumaLog.
/// </summary>
public static class SerilogExtensions
{
    /// <summary>
    /// Enriches log events with LumaLog context (trace ID, span ID, user info, etc.).
    /// </summary>
    public static LoggerConfiguration WithLumaLogContext(
        this LoggerEnrichmentConfiguration enrichmentConfiguration,
        IServiceProvider serviceProvider)
    {
        return enrichmentConfiguration.With(new LumaLogContextEnricher(serviceProvider));
    }

    /// <summary>
    /// Configures Serilog to use LumaLog integration.
    /// </summary>
    public static IHostBuilder UseSerilogWithLumaLog(
        this IHostBuilder hostBuilder,
        Action<HostBuilderContext, IServiceProvider, LoggerConfiguration>? configure = null)
    {
        return hostBuilder.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.With(new LumaLogContextEnricher(services));

            configure?.Invoke(context, services, configuration);
        });
    }
}
