using System.ComponentModel.DataAnnotations;

namespace ERMSystem.Application.DTOs
{
    public class LoginDto
    {
        [Required]
        [MinLength(3)]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        [MaxLength(100)]
        public string Password { get; set; } = string.Empty;
    }
}
