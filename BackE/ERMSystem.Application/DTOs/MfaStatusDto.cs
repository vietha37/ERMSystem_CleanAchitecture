using System;

namespace ERMSystem.Application.DTOs
{
    public class MfaStatusDto
    {
        public bool IsEnabled { get; set; }
        public bool IsSetupPending { get; set; }
        public DateTime? EnabledAtUtc { get; set; }
    }
}
