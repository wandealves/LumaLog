using LumaLog.AspNetCore;
using LumaLog.Serilog;
using LumaLog.SqlServer;
using LumaLog.UI;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.With(new LumaLog.Serilog.Enrichers.LumaLogContextEnricher(services))
        .WriteTo.Console()
        .WriteTo.LumaLog(services);
});

// Add LumaLog services
builder.Services.AddLumaLog(options =>
{
    options.Enabled = true;
    options.TracingEnabled = true;
    options.DashboardPath = "/lumalog";
    options.ApplicationName = "LumaLog.Sample.WebApp";
    options.Environment = builder.Environment.EnvironmentName;
    options.CaptureUnhandledExceptions = true;
    options.MinimumLevel = LumaLog.Models.LogLevel.Information;
})
.AddSerilogIntegration()
.AddUI();

// Optionally use SQL Server (commented out - uses in-memory by default)
// .UseSqlServer(builder.Configuration.GetConnectionString("LumaLog")!);

builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseLumaLogStaticFiles();

app.UseRouting();
app.UseAuthorization();

// Use LumaLog middleware
app.UseLumaLog();

app.MapRazorPages();
app.MapLumaLog();

// Add some sample endpoints that can generate logs
app.MapGet("/api/test", () =>
{
    Log.Information("Test endpoint called");
    return Results.Ok(new { message = "Hello from LumaLog Sample!" });
});

app.MapGet("/api/error", () =>
{
    Log.Warning("About to throw an exception");
    throw new InvalidOperationException("This is a test error!");
});

app.MapGet("/api/slow", async () =>
{
    Log.Information("Starting slow operation");
    await Task.Delay(2000);
    Log.Information("Slow operation completed");
    return Results.Ok(new { message = "Slow operation completed" });
});

app.Run();

// Make Program class partial for integration tests
public partial class Program { }
