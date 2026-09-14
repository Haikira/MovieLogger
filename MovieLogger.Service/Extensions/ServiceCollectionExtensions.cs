using Microsoft.Extensions.DependencyInjection;
using MovieLogger.Service.Interfaces;
using MovieLogger.Service.Mapping;
using MovieLogger.Service.Services;

namespace MovieLogger.Service.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMovieLoggerService(this IServiceCollection services)
        {
            services.AddAutoMapper(_ => { }, typeof(MovieProfile).Assembly);

            services.AddScoped<IMovieService, MovieService>();
            services.AddScoped<IGenreService, GenreService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IMovieWatchService, MovieWatchService>();
            services.AddScoped<IMovieListService, MovieListService>();
            services.AddScoped<IUserMovieService, UserMovieService>();
            services.AddScoped<IListMovieService, ListMovieService>();

            return services;
        }
    }
}
