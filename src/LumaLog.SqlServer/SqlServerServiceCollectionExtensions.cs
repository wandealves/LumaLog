using LumaLog.Abstractions;
using LumaLog.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LumaLog.SqlServer;

/// <summary>
/// Extension methods for configuring SQL Server provider.
/// </summary>
public static class SqlServerServiceCollectionExtensions
{
    /// <summary>
    /// Configures LumaLog to use SQL Server for storage.
    /// </summary>
    public static LumaLogBuilder UseSqlServer(this LumaLogBuilder builder, string connectionString)
    {
        builder.Services.RemoveAll<ILogStore>();
        builder.Services.RemoveAll<ITraceStore>();

        builder.Services.AddSingleton<ILogStore>(sp => new SqlServerLogStore(connectionString));
        builder.Services.AddSingleton<ITraceStore>(sp =>
        {
            var logStore = sp.GetService<ILogStore>();
            return new SqlServerTraceStore(connectionString, logStore);
        });

        return builder;
    }
}
