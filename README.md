# LumaLog

A .NET library for error logging and distributed tracing with a modern UI dashboard and native Serilog integration.

## Features

- **Error Logging**: Capture and store application logs with rich metadata
- **Distributed Tracing**: Track requests across services with trace and span correlation
- **Modern Dashboard**: Beautiful UI built with Tailwind CSS for viewing logs and traces
- **Serilog Integration**: Native integration with Serilog for seamless logging
- **Multiple Database Providers**: SQL Server, MySQL, PostgreSQL support
- **Export Capabilities**: Export logs to JSON or CSV formats
- **Dark/Light Mode**: Dashboard supports both themes

## Installation

```bash
# Core library
dotnet add package LumaLog

# ASP.NET Core integration
dotnet add package LumaLog.AspNetCore

# Dashboard UI
dotnet add package LumaLog.UI

# Serilog integration
dotnet add package LumaLog.Serilog

# Database providers (choose one)
dotnet add package LumaLog.SqlServer
dotnet add package LumaLog.MySql
dotnet add package LumaLog.PostgreSql
```

## Quick Start

### Basic Setup

```csharp
using LumaLog.AspNetCore;
using LumaLog.UI;

var builder = WebApplication.CreateBuilder(args);

// Add LumaLog services
builder.Services.AddLumaLog(options =>
{
    options.Enabled = true;
    options.TracingEnabled = true;
    options.DashboardPath = "/lumalog";
    options.ApplicationName = "MyApp";
})
.AddUI();

var app = builder.Build();

// Use LumaLog middleware
app.UseLumaLog();

// Map dashboard endpoints
app.MapLumaLog();

app.Run();
```

### With Serilog

```csharp
using LumaLog.AspNetCore;
using LumaLog.Serilog;
using LumaLog.UI;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog with LumaLog
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.With(new LumaLogContextEnricher(services))
        .WriteTo.Console()
        .WriteTo.LumaLog(services);
});

// Add LumaLog services
builder.Services.AddLumaLog(options =>
{
    options.Enabled = true;
    options.TracingEnabled = true;
    options.ApplicationName = "MyApp";
})
.AddSerilogIntegration()
.AddUI();

var app = builder.Build();

app.UseLumaLog();
app.MapLumaLog();

app.Run();
```

### With SQL Server

```csharp
builder.Services.AddLumaLog(options =>
{
    options.Enabled = true;
})
.UseSqlServer(builder.Configuration.GetConnectionString("LumaLog")!)
.AddUI();
```

## Configuration Options

```csharp
builder.Services.AddLumaLog(options =>
{
    // General
    options.Enabled = true;
    options.TracingEnabled = true;
    options.ApplicationName = "MyApp";
    options.Environment = "Production";

    // Dashboard
    options.DashboardPath = "/lumalog";
    options.RequireAuthentication = true;
    options.AllowedRoles = new List<string> { "Admin" };

    // Logging
    options.MinimumLevel = LogLevel.Information;
    options.CaptureUnhandledExceptions = true;
    options.IncludeMachineName = true;

    // Request filtering
    options.IgnorePaths = new List<string> { "/health", "/metrics" };
    options.CaptureHeaders = new List<string> { "User-Agent", "X-Request-Id" };

    // Retention
    options.RetentionDays = 30;
    options.EnableAutoCleanup = true;

    // Batching
    options.BatchSize = 100;
    options.BatchFlushIntervalMs = 5000;
});
```

## Dashboard

Access the dashboard at `/lumalog` (configurable). Features include:

- **Dashboard**: Overview with statistics and charts
- **Logs**: Searchable, filterable log list with detail view
- **Traces**: Distributed trace timeline visualization
- **Export**: Download logs and traces as JSON or CSV

## API Endpoints

The dashboard exposes REST API endpoints:

```
GET  /lumalog/api/logs              - List logs (paginated)
GET  /lumalog/api/logs/{id}         - Get log details
POST /lumalog/api/logs/{id}/resolve - Mark log as resolved
DELETE /lumalog/api/logs/{id}       - Delete log

GET  /lumalog/api/traces            - List traces (paginated)
GET  /lumalog/api/traces/{traceId}  - Get trace details

GET  /lumalog/api/statistics/logs   - Get log statistics
GET  /lumalog/api/statistics/traces - Get trace statistics

GET  /lumalog/api/export/logs       - Export logs (JSON/CSV)
GET  /lumalog/api/export/traces     - Export traces (JSON/CSV)
```

## Manual Logging

```csharp
public class MyService
{
    private readonly ILumaLogService _lumaLog;

    public MyService(ILumaLogService lumaLog)
    {
        _lumaLog = lumaLog;
    }

    public async Task DoSomething()
    {
        try
        {
            await _lumaLog.LogInfoAsync("Starting operation", new Dictionary<string, object>
            {
                ["userId"] = "123",
                ["action"] = "process"
            });

            // ... do work
        }
        catch (Exception ex)
        {
            await _lumaLog.LogErrorAsync(ex, "Operation failed");
            throw;
        }
    }
}
```

## Manual Tracing

```csharp
public class MyService
{
    private readonly ITraceManager _traceManager;

    public MyService(ITraceManager traceManager)
    {
        _traceManager = traceManager;
    }

    public async Task ProcessOrder(int orderId)
    {
        using var span = _traceManager.StartSpan("ProcessOrder");
        span.SetTag("orderId", orderId.ToString());

        try
        {
            // ... process order
            span.SetStatus(SpanStatus.Ok);
        }
        catch (Exception ex)
        {
            span.RecordException(ex);
            throw;
        }
    }
}
```

## Project Structure

```
LumaLog/
├── src/
│   ├── LumaLog/                  # Core library
│   ├── LumaLog.AspNetCore/       # ASP.NET Core integration
│   ├── LumaLog.UI/               # Dashboard UI
│   ├── LumaLog.Serilog/          # Serilog integration
│   ├── LumaLog.SqlServer/        # SQL Server provider
│   ├── LumaLog.MySql/            # MySQL provider
│   └── LumaLog.PostgreSql/       # PostgreSQL provider
├── samples/
│   ├── LumaLog.Sample.WebApp/    # Web app sample
│   └── LumaLog.Sample.Api/       # API sample
└── tests/
    ├── LumaLog.Tests/            # Unit tests
    └── LumaLog.IntegrationTests/ # Integration tests
```

## Building

```bash
# Restore and build
dotnet build

# Run tests
dotnet test

# Run sample app
cd samples/LumaLog.Sample.WebApp
dotnet run
```

Then open http://localhost:5000 and navigate to `/lumalog` for the dashboard.

## Database Setup

### SQL Server

Tables are created automatically on first run. Manual script:

```sql
-- See src/LumaLog.SqlServer/Scripts/CreateTables.sql
```

## License

MIT License

## Contributing

Contributions are welcome! Please open an issue or submit a pull request.
