using System.ComponentModel.DataAnnotations;

namespace MovieLogger.Service.Dtos.Movies
{
    public class CreateMovieDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Range(typeof(DateOnly), "1888-01-01", "9999-12-31")]
        public DateOnly ReleaseDate { get; set; }

        [MaxLength(200)]
        public string? Director { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        public List<int> GenreIds { get; set; } = [];
    }
}
