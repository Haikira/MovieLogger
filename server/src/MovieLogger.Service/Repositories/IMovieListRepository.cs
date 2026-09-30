using MovieLogger.Service.Entities;

namespace MovieLogger.Service.Repositories
{
    public interface IMovieListRepository : IRepository<MovieList>
    {
        Task<IReadOnlyList<MovieList>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    }
}
