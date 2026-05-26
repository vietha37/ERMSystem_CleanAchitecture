using System.ComponentModel.DataAnnotations;

namespace ERMSystem.Application.DTOs
{
    public class VerifyMfaLoginDto
    {
        [Required]
        public string MfaChallengeToken { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        [MaxLength(8)]
        public string Code { get; set; } = string.Empty;
    }
}
