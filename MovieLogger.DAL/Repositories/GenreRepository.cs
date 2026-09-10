using Microsoft.EntityFrameworkCore;
using MovieLogger.Service.Entities;
using MovieLogger.Service.Repositories;

namespace MovieLogger.DAL.Repositories
{
    public class GenreRepository(MovieLoggerDbContext context) : IGenreRepository
    {
        public async Task<IReadOnlyList<Genre>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await context.Genres
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<Genre?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await context.Genres
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<Genre>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
        {
            var idList = ids.ToList();
            return await context.Genres
                .Where(g => idList.Contains(g.Id))
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Genre genre, CancellationToken cancellationToken = default)
        {
            context.Genres.Add(genre);
            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(Genre genre, CancellationToken cancellationToken = default)
        {
            context.Genres.Update(genre);
            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var genre = await context.Genres.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
            if (genre is null)
            {
                return false;
            }

            context.Genres.Remove(genre);
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
