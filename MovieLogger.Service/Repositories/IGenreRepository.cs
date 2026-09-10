using MovieLogger.Service.Entities;

namespace MovieLogger.Service.Repositories
{
    public interface IGenreRepository
    {
        Task<IReadOnlyList<Genre>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<Genre?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Genre>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);

        Task AddAsync(Genre genre, CancellationToken cancellationToken = default);

        Task UpdateAsync(Genre genre, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
