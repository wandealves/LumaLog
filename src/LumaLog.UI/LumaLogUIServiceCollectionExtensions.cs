using LumaLog.AspNetCore;
using LumaLog.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace LumaLog.UI;

/// <summary>
/// Extension methods for configuring LumaLog UI.
/// </summary>
public static class LumaLogUIServiceCollectionExtensions
{
    /// <summary>
    /// Adds LumaLog UI services and Razor Pages.
    /// </summary>
    public static LumaLogBuilder AddUI(this LumaLogBuilder builder)
    {
        builder.Services.AddRazorPages()
            .AddApplicationPart(typeof(LumaLogUIServiceCollectionExtensions).Assembly);

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(LumaLogUIServiceCollectionExtensions).Assembly);

        return builder;
    }

    /// <summary>
    /// Maps LumaLog UI endpoints (Razor Pages and API).
    /// </summary>
    public static IEndpointRouteBuilder MapLumaLog(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<LumaLogOptions>>().Value;

        endpoints.MapRazorPages();
        endpoints.MapControllers();

        return endpoints;
    }

    /// <summary>
    /// Uses LumaLog static files.
    /// </summary>
    public static IApplicationBuilder UseLumaLogStaticFiles(this IApplicationBuilder app)
    {
        var assembly = typeof(LumaLogUIServiceCollectionExtensions).Assembly;
        var embeddedFileProvider = new EmbeddedFileProvider(assembly, "LumaLog.UI.wwwroot");

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = embeddedFileProvider,
            RequestPath = "/lumalog"
        });

        return app;
    }
}
