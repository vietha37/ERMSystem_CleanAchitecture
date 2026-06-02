using System;

namespace ERMSystem.Application.DTOs
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool RequiresTwoFactor { get; set; }
        public bool IsMfaEnabled { get; set; }
        public string? MfaChallengeToken { get; set; }
        public DateTime? MfaChallengeExpiresAtUtc { get; set; }
    }
}
