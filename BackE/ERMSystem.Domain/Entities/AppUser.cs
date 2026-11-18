using System;

namespace ERMSystem.Domain.Entities
{
    public class AppUser
    {
        private string _passwordHash = string.Empty;

        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Password hash can only be initialized on construction and mutated via domain method.
        /// </summary>
        public string PasswordHash
        {
            get => _passwordHash;
            init => _passwordHash = value ?? string.Empty;
        }

        public string Role { get; set; } = string.Empty;
        public bool MfaEnabled { get; set; }
        public string? MfaSecretProtected { get; set; }
        public DateTime? MfaEnabledAt { get; set; }
        public string? RefreshTokenHash { get; set; }
        public DateTime? RefreshTokenExpiresAt { get; set; }
        public DateTime? RefreshTokenRevokedAt { get; set; }

        public void UpdatePasswordHash(string newPasswordHash)
        {
            if (string.IsNullOrWhiteSpace(newPasswordHash))
            {
                throw new ArgumentException("Password hash cannot be empty or whitespace.", nameof(newPasswordHash));
            }

            _passwordHash = newPasswordHash;
        }
    }
}
