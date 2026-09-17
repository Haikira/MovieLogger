using System.ComponentModel.DataAnnotations;

namespace MovieLogger.Service.Dtos.Lists
{
    public class UpdateMovieListDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }
    }
}
