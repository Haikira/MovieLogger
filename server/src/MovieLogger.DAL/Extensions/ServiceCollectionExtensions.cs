using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MovieLogger.DAL.Repositories;
using MovieLogger.Service.Repositories;

namespace MovieLogger.DAL.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public const string ConnectionStringName = "MovieLoggerDb";

        public static IServiceCollection AddMovieLoggerDal(this IServiceCollection services, IConfiguration configuration)
        {
            // Fail at startup rather than on the first database request. Development gets the local
            // connection string from appsettings.Development.json; every other environment must supply
            // it, e.g. through the ConnectionStrings__MovieLoggerDb environment variable.
            var connectionString = configuration.GetConnectionString(ConnectionStringName);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"ConnectionStrings:{ConnectionStringName} is not configured. Outside Development, supply it " +
                    $"through the ConnectionStrings__{ConnectionStringName} environment variable.");
            }

            services.AddDbContext<MovieLoggerDbContext>(options => options.UseSqlServer(connectionString));

            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IMovieRepository, MovieRepository>();
            services.AddScoped<IGenreRepository, GenreRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IMovieWatchRepository, MovieWatchRepository>();
            services.AddScoped<IMovieListRepository, MovieListRepository>();
            services.AddScoped<IUserMovieRepository, UserMovieRepository>();
            services.AddScoped<IListMovieRepository, ListMovieRepository>();
            services.AddScoped<IWatchlistItemRepository, WatchlistItemRepository>();

            return services;
        }
    }
}