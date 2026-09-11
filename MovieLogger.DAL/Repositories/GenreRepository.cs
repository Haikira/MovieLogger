using Microsoft.EntityFrameworkCore;
using MovieLogger.Service.Entities;
using MovieLogger.Service.Repositories;

namespace MovieLogger.DAL.Repositories
{
    public class GenreRepository(MovieLoggerDbContext context) : Repository<Genre>(context), IGenreRepository
    {
        public async Task<IReadOnlyList<Genre>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
        {
            var idList = ids.ToList();
            return await Context.Genres
                .Where(g => idList.Contains(g.Id))
                .ToListAsync(cancellationToken);
        }
    }
}
