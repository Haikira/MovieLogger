using System.ComponentModel.DataAnnotations;

namespace MovieLogger.Service.Dtos.Movies
{
    public class UpdateMovieDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Range(1888, 2200)]
        public int ReleaseYear { get; set; }

        [Range(1, 1000)]
        public int? RuntimeMinutes { get; set; }

        [MaxLength(200)]
        public string? Director { get; set; }

        [MaxLength(2000)]
        public string? Synopsis { get; set; }

        [MaxLength(2000)]
        [Url]
        public string? PosterImageUrl { get; set; }

        public List<int> GenreIds { get; set; } = [];
    }
}
