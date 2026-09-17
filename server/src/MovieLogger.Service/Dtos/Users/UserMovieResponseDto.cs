using MovieLogger.Service.Dtos.Movies;

namespace MovieLogger.Service.Dtos.Users
{
    public class UserMovieResponseDto
    {
        public int MovieId { get; set; }

        public bool IsFavourite { get; set; }

        public bool IsOwned { get; set; }

        public MovieResponseDto Movie { get; set; } = null!;
    }
}
