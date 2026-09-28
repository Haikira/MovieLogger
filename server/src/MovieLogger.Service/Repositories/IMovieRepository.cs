using MovieLogger.Service.Dtos.Movies;
using MovieLogger.Service.Entities;

namespace MovieLogger.Service.Repositories
{
    public interface IMovieRepository : IRepository<Movie>
    {
        Task<(IReadOnlyList<Movie> Items, int TotalCount)> SearchAsync(MovieSearchQueryDto query, CancellationToken cancellationToken = default);
    }
}
