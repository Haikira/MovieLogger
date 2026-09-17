using Microsoft.EntityFrameworkCore;
using MovieLogger.Service.Repositories;

namespace MovieLogger.DAL.Repositories
{
    public class Repository<T>(MovieLoggerDbContext context) : IRepository<T> where T : class
    {
        protected MovieLoggerDbContext Context { get; } = context;

        public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await Context.Set<T>().AsNoTracking().ToListAsync(cancellationToken);
        }

        public virtual async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await Context.Set<T>().FindAsync([id], cancellationToken);
        }

        public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            Context.Set<T>().Add(entity);
            await Context.SaveChangesAsync(cancellationToken);
        }

        public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            Context.Set<T>().Update(entity);
            await Context.SaveChangesAsync(cancellationToken);
        }

        public virtual async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await Context.Set<T>().FindAsync([id], cancellationToken);
            if (entity is null)
            {
                return false;
            }

            Context.Set<T>().Remove(entity);
            await Context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
