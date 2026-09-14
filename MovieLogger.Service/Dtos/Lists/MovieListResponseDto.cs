using MovieLogger.Service.Dtos.Movies;

namespace MovieLogger.Service.Dtos.Lists
{
    public class MovieListResponseDto
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; }

        public List<MovieResponseDto> Movies { get; set; } = [];
    }
}
