using LumaLog.Abstractions;
using LumaLog.AspNetCore;
using LumaLog.Sample.Api.Servicos;
using LumaLog.UI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<CalculatorService>();

// Add LumaLog with in-memory storage and UI
builder.Services.AddLumaLog(options =>
{
    options.Enabled = true;
    options.TracingEnabled = true;
    options.DashboardPath = "/lumalog";
    options.ApplicationName = "LumaLog.Sample.Api";
    options.Environment = builder.Environment.EnvironmentName;
    options.IgnorePaths = ["/swagger", "/health", "/favicon.ico"];
    options.CaptureHeaders = ["User-Agent", "Referer", "X-Forwarded-For", "X-Request-Id", "Content-Type", "Accept"];
    options.ExcludeHeaders = ["Authorization", "Cookie", "X-Api-Key"];
})
.UseInMemory()
.AddUI();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Use LumaLog middleware (captures requests, traces, errors)
app.UseLumaLog();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

// Weather forecast endpoint
app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

// Demo endpoints to generate logs
app.MapGet("/api/demo/success", () =>
{
    return Results.Ok(new { message = "Operation completed successfully", timestamp = DateTime.UtcNow });
})
.WithName("DemoSuccess")
.WithTags("Demo");

app.MapGet("/api/demo/warning", (ILumaLogService lumaLog) =>
{
    // Manually log a warning
    lumaLog.LogAsync(new LumaLog.Models.LogEntry
    {
        Level = LumaLog.Models.LogLevel.Warning,
        Message = "This is a demo warning message",
        Source = "DemoController",
        CreatedAt = DateTimeOffset.UtcNow
    });

    return Results.Ok(new { message = "Warning logged", timestamp = DateTime.UtcNow });
})
.WithName("DemoWarning")
.WithTags("Demo");

app.MapGet("/api/demo/error", (CalculatorService service) =>
    {
        service.Divide(0);
    })
.WithName("DemoError")
.WithTags("Demo");

app.MapGet("/api/demo/notfound", () =>
{
    return Results.NotFound(new { error = "Resource not found", code = "NOT_FOUND" });
})
.WithName("DemoNotFound")
.WithTags("Demo");

app.MapPost("/api/demo/create", (CreateItemRequest request) =>
{
    if (string.IsNullOrEmpty(request.Name))
    {
        return Results.BadRequest(new { error = "Name is required" });
    }

    return Results.Created($"/api/items/{Guid.NewGuid()}", new { id = Guid.NewGuid(), name = request.Name });
})
.WithName("DemoCreate")
.WithTags("Demo");

app.MapGet("/api/demo/slow", async () =>
{
    // Simulate a slow operation
    await Task.Delay(Random.Shared.Next(500, 2000));
    return Results.Ok(new { message = "Slow operation completed", timestamp = DateTime.UtcNow });
})
.WithName("DemoSlow")
.WithTags("Demo");

app.MapGet("/api/demo/random-error", () =>
{
    var random = Random.Shared.Next(1, 4);
    return random switch
    {
        1 => throw new ArgumentException("Invalid argument provided"),
        2 => throw new InvalidOperationException("Invalid operation state"),
        _ => Results.Ok(new { message = "Lucky! No error this time.", timestamp = DateTime.UtcNow })
    };
})
.WithName("DemoRandomError")
.WithTags("Demo");

// Health check endpoint (excluded from logging)
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
.WithName("Health")
.WithTags("Health");

// Map LumaLog UI endpoints
app.MapLumaLog();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

record CreateItemRequest(string? Name, string? Description);
