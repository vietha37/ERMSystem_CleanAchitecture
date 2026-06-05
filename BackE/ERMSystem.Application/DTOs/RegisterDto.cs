using System.ComponentModel.DataAnnotations;

namespace ERMSystem.Application.DTOs
{
    public class RegisterDto
    {
        [Required]
        [MinLength(3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [MinLength(3)]
        public string? Name { get; set; }

        [Required]
        [RegularExpression("^(Admin|Doctor|Cashier)$",
            ErrorMessage = "Role must be Admin, Doctor, or Cashier.")]
        public string Role { get; set; } = string.Empty;
    }
}
