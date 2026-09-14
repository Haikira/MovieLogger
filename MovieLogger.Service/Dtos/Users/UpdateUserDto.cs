using System.ComponentModel.DataAnnotations;

namespace MovieLogger.Service.Dtos.Users
{
    public class UpdateUserDto
    {
        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;
    }
}
