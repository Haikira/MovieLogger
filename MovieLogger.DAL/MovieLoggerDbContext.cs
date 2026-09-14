using Microsoft.EntityFrameworkCore;
using MovieLogger.Service.Entities;

namespace MovieLogger.DAL
{
    public class MovieLoggerDbContext(DbContextOptions<MovieLoggerDbContext> options) : DbContext(options)
    {
        public DbSet<Movie> Movies => Set<Movie>();

        public DbSet<Genre> Genres => Set<Genre>();

        public DbSet<User> Users => Set<User>();

        public DbSet<MovieWatch> MovieWatches => Set<MovieWatch>();

        public DbSet<MovieList> Lists => Set<MovieList>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(MovieLoggerDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
