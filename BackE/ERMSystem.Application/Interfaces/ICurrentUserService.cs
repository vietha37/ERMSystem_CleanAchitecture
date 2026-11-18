using System;

namespace ERMSystem.Application.Interfaces;

/// <summary>
/// Abstraction for accessing current user context without leaking HTTP/presentation concerns into Application layer.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Username { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}
