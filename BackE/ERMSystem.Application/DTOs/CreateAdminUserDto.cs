using System.ComponentModel.DataAnnotations;

namespace ERMSystem.Application.DTOs
{
    public class CreateAdminUserDto
    {
        [Required]
        [MinLength(3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [MinLength(3)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(Doctor|Cashier)$",
            ErrorMessage = "Role must be Doctor or Cashier.")]
        public string Role { get; set; } = string.Empty;
    }
}
