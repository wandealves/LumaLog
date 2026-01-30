using LumaLog.Abstractions;
using LumaLog.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LumaLog.AspNetCore;

/// <summary>
/// Extension methods for configuring LumaLog middleware.
/// </summary>
public static class LumaLogApplicationBuilderExtensions
{
    /// <summary>
    /// Adds LumaLog middleware to the pipeline.
    /// </summary>
    public static IApplicationBuilder UseLumaLog(this IApplicationBuilder app)
    {
        // Ensure tables exist
        var logStore = app.ApplicationServices.GetService<ILogStore>();
        var traceStore = app.ApplicationServices.GetService<ITraceStore>();

        logStore?.EnsureTablesExistAsync().GetAwaiter().GetResult();
        traceStore?.EnsureTablesExistAsync().GetAwaiter().GetResult();

        return app.UseMiddleware<LumaLogMiddleware>();
    }

    /// <summary>
    /// Maps LumaLog dashboard endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapLumaLogDashboard(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<LumaLogOptions>>().Value;
        var basePath = options.DashboardPath.TrimEnd('/');

        var group = endpoints.MapGroup(basePath);

        // Apply authorization if required
        if (options.RequireAuthentication)
        {
            group.RequireAuthorization();
        }

        return endpoints;
    }
}
