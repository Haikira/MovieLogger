using System.ComponentModel.DataAnnotations;

namespace MovieLogger.Service.Dtos.Movies
{
    public class UpdateMovieDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Range(1888, 9999)]
        public int ReleaseYear { get; set; }

        [MaxLength(200)]
        public string? Director { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        public List<int> GenreIds { get; set; } = [];
    }
}
