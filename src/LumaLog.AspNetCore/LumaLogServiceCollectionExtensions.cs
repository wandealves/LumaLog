using LumaLog.Abstractions;
using LumaLog.Configuration;
using LumaLog.Services;
using LumaLog.Services.Exporters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LumaLog.AspNetCore;

/// <summary>
/// Extension methods for configuring LumaLog services.
/// </summary>
public static class LumaLogServiceCollectionExtensions
{
    /// <summary>
    /// Adds LumaLog services to the service collection.
    /// </summary>
    public static LumaLogBuilder AddLumaLog(this IServiceCollection services, Action<LumaLogOptions>? configure = null)
    {
        // Configure options
        if (configure != null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<LumaLogOptions>(_ => { });
        }

        // Register core services
        services.TryAddSingleton<ITraceManager, TraceManager>();
        services.TryAddSingleton<ILumaLogService, LumaLogService>();

        // Register default in-memory stores (can be replaced by database providers)
        services.TryAddSingleton<ILogStore, InMemoryLogStore>();
        services.TryAddSingleton<ITraceStore, InMemoryTraceStore>();

        // Register exporters
        services.AddSingleton<IExporter, JsonExporter>();
        services.AddSingleton<IExporter, CsvExporter>();

        return new LumaLogBuilder(services);
    }
}

/// <summary>
/// Builder for configuring LumaLog.
/// </summary>
public class LumaLogBuilder
{
    public IServiceCollection Services { get; }

    public LumaLogBuilder(IServiceCollection services)
    {
        Services = services;
    }

    /// <summary>
    /// Uses in-memory storage (default, for testing).
    /// </summary>
    public LumaLogBuilder UseInMemory()
    {
        Services.RemoveAll<ILogStore>();
        Services.RemoveAll<ITraceStore>();

        Services.AddSingleton<ILogStore, InMemoryLogStore>();
        Services.AddSingleton<ITraceStore, InMemoryTraceStore>();

        return this;
    }

    /// <summary>
    /// Adds a custom log store.
    /// </summary>
    public LumaLogBuilder UseLogStore<T>() where T : class, ILogStore
    {
        Services.RemoveAll<ILogStore>();
        Services.AddSingleton<ILogStore, T>();
        return this;
    }

    /// <summary>
    /// Adds a custom trace store.
    /// </summary>
    public LumaLogBuilder UseTraceStore<T>() where T : class, ITraceStore
    {
        Services.RemoveAll<ITraceStore>();
        Services.AddSingleton<ITraceStore, T>();
        return this;
    }

    /// <summary>
    /// Adds a notifier.
    /// </summary>
    public LumaLogBuilder AddNotifier<T>() where T : class, INotifier
    {
        Services.AddSingleton<INotifier, T>();
        return this;
    }

    /// <summary>
    /// Adds an exporter.
    /// </summary>
    public LumaLogBuilder AddExporter<T>() where T : class, IExporter
    {
        Services.AddSingleton<IExporter, T>();
        return this;
    }
}
