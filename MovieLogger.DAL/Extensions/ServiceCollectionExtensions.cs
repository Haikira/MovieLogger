using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MovieLogger.DAL.Repositories;
using MovieLogger.Service.Repositories;

namespace MovieLogger.DAL.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMovieLoggerDal(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<MovieLoggerDbContext>(options =>
                options.UseSqlite(configuration.GetConnectionString("MovieLoggerDb")));

            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IMovieRepository, MovieRepository>();
            services.AddScoped<IGenreRepository, GenreRepository>();

            return services;
        }
    }
}
