using System;

namespace ERMSystem.Application.DTOs
{
    public class MfaSetupResponseDto
    {
        public string ManualEntryKey { get; set; } = string.Empty;
        public string OtpAuthUri { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
    }
}
