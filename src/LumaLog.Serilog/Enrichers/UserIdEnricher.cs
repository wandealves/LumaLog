using LumaLog.AspNetCore;
using Serilog.Core;
using Serilog.Events;

namespace LumaLog.Serilog.Enrichers;

/// <summary>
/// Enriches log events with the current user ID.
/// </summary>
public class UserIdEnricher : ILogEventEnricher
{
    private readonly UserContextProvider _userContextProvider;

    public UserIdEnricher(UserContextProvider userContextProvider)
    {
        _userContextProvider = userContextProvider;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var userId = _userContextProvider.GetUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserId", userId));
        }

        var userName = _userContextProvider.GetUserName();
        if (!string.IsNullOrEmpty(userName))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserName", userName));
        }
    }
}
