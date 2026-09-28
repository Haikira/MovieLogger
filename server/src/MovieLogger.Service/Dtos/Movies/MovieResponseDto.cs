using MovieLogger.Service.Dtos.Genres;

namespace MovieLogger.Service.Dtos.Movies
{
    public class MovieResponseDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public int ReleaseYear { get; set; }

        public int? RuntimeMinutes { get; set; }

        public string? Director { get; set; }

        public string? Synopsis { get; set; }

        public string? PosterImageUrl { get; set; }

        public DateTime CreatedAt { get; set; }

        public int? CreatedByUserId { get; set; }

        public List<GenreResponseDto> Genres { get; set; } = [];
    }
}
