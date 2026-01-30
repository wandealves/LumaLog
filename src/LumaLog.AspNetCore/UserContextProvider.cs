using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace LumaLog.AspNetCore;

/// <summary>
/// Provides user context information from HttpContext.
/// </summary>
public class UserContextProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContextProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Gets the current user's ID.
    /// </summary>
    public string? GetUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        return user.FindFirst("sub")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("id")?.Value;
    }

    /// <summary>
    /// Gets the current user's name.
    /// </summary>
    public string? GetUserName()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        return user.Identity.Name
            ?? user.FindFirst("name")?.Value
            ?? user.FindFirst(ClaimTypes.Name)?.Value;
    }

    /// <summary>
    /// Gets the current user's email.
    /// </summary>
    public string? GetUserEmail()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        return user.FindFirst("email")?.Value
            ?? user.FindFirst(ClaimTypes.Email)?.Value;
    }

    /// <summary>
    /// Gets whether the current user is authenticated.
    /// </summary>
    public bool IsAuthenticated()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
    }

    /// <summary>
    /// Gets whether the current user has a specific role.
    /// </summary>
    public bool IsInRole(string role)
    {
        return _httpContextAccessor.HttpContext?.User?.IsInRole(role) == true;
    }

    /// <summary>
    /// Gets all claims for the current user.
    /// </summary>
    public IEnumerable<Claim> GetClaims()
    {
        return _httpContextAccessor.HttpContext?.User?.Claims ?? Enumerable.Empty<Claim>();
    }
}
