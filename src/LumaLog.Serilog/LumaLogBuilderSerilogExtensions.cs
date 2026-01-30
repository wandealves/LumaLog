using LumaLog.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LumaLog.Serilog;

/// <summary>
/// Extension methods for adding Serilog integration to LumaLog.
/// </summary>
public static class LumaLogBuilderSerilogExtensions
{
    /// <summary>
    /// Adds Serilog integration services.
    /// </summary>
    public static LumaLogBuilder AddSerilogIntegration(this LumaLogBuilder builder)
    {
        // Ensure HttpContextAccessor is registered
        builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        // Register context providers
        builder.Services.TryAddScoped<HttpContextTraceAccessor>();
        builder.Services.TryAddScoped<UserContextProvider>();

        return builder;
    }
}
