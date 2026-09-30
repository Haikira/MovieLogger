using MovieLogger.Service.Entities;
using MovieLogger.Service.Interfaces;
using MovieLogger.Service.Repositories;

namespace MovieLogger.Service.Services
{
    public class ListMovieService(
        IListMovieRepository listMovieRepository,
        IMovieListRepository movieListRepository,
        IMovieRepository movieRepository) : IListMovieService
    {
        public async Task<AddMovieToListResult> AddAsync(int listId, int movieId, int userId, CancellationToken cancellationToken = default)
        {
            var list = await movieListRepository.GetByIdAsync(listId, cancellationToken);
            if (list is null || list.UserId != userId)
            {
                return AddMovieToListResult.ListNotFound();
            }

            var movie = await movieRepository.GetByIdAsync(movieId, cancellationToken);
            if (movie is null)
            {
                return AddMovieToListResult.MovieNotFound();
            }

            var existing = await listMovieRepository.GetAsync(listId, movieId, cancellationToken);
            if (existing is not null)
            {
                return AddMovieToListResult.Success();
            }

            await listMovieRepository.AddAsync(new ListMovie
            {
                ListId = listId,
                MovieId = movieId,
                AddedAt = DateTime.UtcNow
            }, cancellationToken);

            return AddMovieToListResult.Success();
        }

        public async Task<bool> RemoveAsync(int listId, int movieId, int userId, CancellationToken cancellationToken = default)
        {
            var list = await movieListRepository.GetByIdAsync(listId, cancellationToken);
            if (list is null || list.UserId != userId)
            {
                return false;
            }

            return await listMovieRepository.DeleteAsync(listId, movieId, cancellationToken);
        }
    }
}
