using System.ComponentModel.DataAnnotations;

namespace ERMSystem.Application.DTOs
{
    public class VerifyMfaCodeDto
    {
        [Required]
        [MinLength(6)]
        [MaxLength(8)]
        public string Code { get; set; } = string.Empty;
    }
}
