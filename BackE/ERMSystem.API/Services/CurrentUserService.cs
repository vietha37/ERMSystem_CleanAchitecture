using System;
using System.Security.Claims;
using ERMSystem.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ERMSystem.API.Services;

/// <summary>
/// Resolves current authenticated user claims from HTTP context.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null) return null;

            var idClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? user.FindFirstValue("sub")
                          ?? user.FindFirstValue(ClaimTypes.Name);

            return Guid.TryParse(idClaim, out var parsed) ? parsed : null;
        }
    }

    public string? Username
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirstValue(ClaimTypes.Name)
                   ?? user?.FindFirstValue(ClaimTypes.Upn)
                   ?? user?.FindFirstValue("unique_name")
                   ?? user?.Identity?.Name;
        }
    }

    public string? Role
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirstValue(ClaimTypes.Role)
                   ?? user?.FindFirstValue("role");
        }
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
