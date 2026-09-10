using MovieLogger.Service.Entities;

namespace MovieLogger.Service.Repositories
{
    public interface IMovieRepository
    {
        Task<IReadOnlyList<Movie>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<Movie?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task AddAsync(Movie movie, CancellationToken cancellationToken = default);

        Task UpdateAsync(Movie movie, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
