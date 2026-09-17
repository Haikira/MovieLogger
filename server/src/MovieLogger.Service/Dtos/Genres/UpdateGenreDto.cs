using System.ComponentModel.DataAnnotations;

namespace MovieLogger.Service.Dtos.Genres
{
    public class UpdateGenreDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}
