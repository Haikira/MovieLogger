using AutoMapper;
using MovieLogger.Service.Dtos.Movies;
using MovieLogger.Service.Entities;
using MovieLogger.Service.Interfaces;
using MovieLogger.Service.Repositories;

namespace MovieLogger.Service.Services
{
    public class MovieService(IMovieRepository movieRepository, IGenreRepository genreRepository, IMapper mapper) : IMovieService
    {
        public async Task<IReadOnlyList<MovieResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var movies = await movieRepository.GetAllAsync(cancellationToken);
            return movies.Select(mapper.Map<MovieResponseDto>).ToList();
        }

        public async Task<MovieResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var movie = await movieRepository.GetByIdAsync(id, cancellationToken);
            return movie is null ? null : mapper.Map<MovieResponseDto>(movie);
        }

        public async Task<MovieMutationResult> CreateAsync(CreateMovieDto dto, CancellationToken cancellationToken = default)
        {
            var (genres, invalidGenreIds) = await ResolveGenresAsync(dto.GenreIds, cancellationToken);
            if (invalidGenreIds.Count > 0)
            {
                return MovieMutationResult.InvalidGenres(invalidGenreIds);
            }

            var movie = mapper.Map<Movie>(dto);
            movie.Genres = genres;

            await movieRepository.AddAsync(movie, cancellationToken);
            return MovieMutationResult.Success(mapper.Map<MovieResponseDto>(movie));
        }

        public async Task<MovieMutationResult> UpdateAsync(int id, UpdateMovieDto dto, CancellationToken cancellationToken = default)
        {
            var movie = await movieRepository.GetByIdAsync(id, cancellationToken);
            if (movie is null)
            {
                return MovieMutationResult.NotFound();
            }

            var (genres, invalidGenreIds) = await ResolveGenresAsync(dto.GenreIds, cancellationToken);
            if (invalidGenreIds.Count > 0)
            {
                return MovieMutationResult.InvalidGenres(invalidGenreIds);
            }

            mapper.Map(dto, movie);

            movie.Genres.Clear();
            foreach (var genre in genres)
            {
                movie.Genres.Add(genre);
            }

            await movieRepository.UpdateAsync(movie, cancellationToken);
            return MovieMutationResult.Success(mapper.Map<MovieResponseDto>(movie));
        }

        public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            return movieRepository.DeleteAsync(id, cancellationToken);
        }

        private async Task<(List<Genre> Genres, List<int> InvalidGenreIds)> ResolveGenresAsync(
            IReadOnlyList<int> genreIds, CancellationToken cancellationToken)
        {
            if (genreIds.Count == 0)
            {
                return ([], []);
            }

            var genres = await genreRepository.GetByIdsAsync(genreIds, cancellationToken);
            var foundIds = genres.Select(g => g.Id).ToHashSet();
            var invalidGenreIds = genreIds.Where(id => !foundIds.Contains(id)).Distinct().ToList();

            return (genres.ToList(), invalidGenreIds);
        }
    }
}
