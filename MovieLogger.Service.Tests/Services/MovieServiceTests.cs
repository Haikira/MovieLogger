using AutoMapper;
using FluentAssertions;
using MovieLogger.Service.Dtos.Movies;
using MovieLogger.Service.Entities;
using MovieLogger.Service.Repositories;
using MovieLogger.Service.Services;
using MovieLogger.Service.Tests.TestSupport;
using NSubstitute;

namespace MovieLogger.Service.Tests.Services
{
    public class MovieServiceTests
    {
        private readonly IMovieRepository _movieRepository = Substitute.For<IMovieRepository>();
        private readonly IGenreRepository _genreRepository = Substitute.For<IGenreRepository>();
        private readonly IMapper _mapper = TestMapperFactory.Create();
        private readonly MovieService _sut;

        public MovieServiceTests()
        {
            _sut = new MovieService(_movieRepository, _genreRepository, _mapper);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsMoviesMappedToResponseDtos()
        {
            var movies = new List<Movie>
            {
                new() { Id = 1, Title = "The Matrix", ReleaseYear = 1999, Genres = [new Genre { Id = 1, Name = "Sci-Fi" }] },
                new() { Id = 2, Title = "Heat", ReleaseYear = 1995, Genres = [] },
            };
            _movieRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(movies);

            var result = await _sut.GetAllAsync();

            result.Should().HaveCount(2);
            result[0].Title.Should().Be("The Matrix");
            result[0].Genres.Should().ContainSingle(g => g.Name == "Sci-Fi");
            result[1].Title.Should().Be("Heat");
        }

        [Fact]
        public async Task GetAllAsync_RepositoryReturnsEmpty_ReturnsEmptyList()
        {
            _movieRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Movie>());

            var result = await _sut.GetAllAsync();

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllAsync_ForwardsCancellationTokenToRepository()
        {
            using var cts = new CancellationTokenSource();
            _movieRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Movie>());

            await _sut.GetAllAsync(cts.Token);

            await _movieRepository.Received(1).GetAllAsync(cts.Token);
        }

        [Fact]
        public async Task GetByIdAsync_MovieExists_ReturnsMappedDto()
        {
            var movie = new Movie { Id = 1, Title = "The Matrix", ReleaseYear = 1999 };
            _movieRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(movie);

            var result = await _sut.GetByIdAsync(1);

            result.Should().NotBeNull();
            result!.Title.Should().Be("The Matrix");
        }

        [Fact]
        public async Task GetByIdAsync_MovieDoesNotExist_ReturnsNull()
        {
            _movieRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Movie?)null);

            var result = await _sut.GetByIdAsync(99);

            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateAsync_NoGenreIds_DoesNotQueryGenreRepositoryAndCreatesMovieWithNoGenres()
        {
            var dto = new CreateMovieDto { Title = "Arrival", ReleaseYear = 2016, GenreIds = [] };

            var result = await _sut.CreateAsync(dto);

            await _genreRepository.DidNotReceive().GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>());
            result.Outcome.Should().Be(MovieMutationOutcome.Success);
            result.Movie!.Genres.Should().BeEmpty();
        }

        [Fact]
        public async Task CreateAsync_AllGenreIdsValid_AddsMovieWithResolvedGenres()
        {
            var genres = new List<Genre>
            {
                new() { Id = 1, Name = "Sci-Fi" },
                new() { Id = 2, Name = "Drama" },
            };
            _genreRepository.GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>()).Returns(genres);
            var dto = new CreateMovieDto { Title = "Arrival", ReleaseYear = 2016, GenreIds = [1, 2] };

            var result = await _sut.CreateAsync(dto);

            result.Outcome.Should().Be(MovieMutationOutcome.Success);
            result.Movie!.Genres.Should().HaveCount(2);
            result.Movie.Genres.Select(g => g.Name).Should().BeEquivalentTo("Sci-Fi", "Drama");
            await _movieRepository.Received(1).AddAsync(
                Arg.Is<Movie>(m => m.Title == "Arrival" && m.Genres.Count == 2),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CreateAsync_SomeGenreIdsInvalid_ReturnsInvalidGenreIdsAndDoesNotAddMovie()
        {
            var genres = new List<Genre> { new() { Id = 1, Name = "Sci-Fi" } };
            _genreRepository.GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>()).Returns(genres);
            var dto = new CreateMovieDto { Title = "Arrival", ReleaseYear = 2016, GenreIds = [1, 2, 3] };

            var result = await _sut.CreateAsync(dto);

            result.Outcome.Should().Be(MovieMutationOutcome.InvalidGenreIds);
            result.InvalidGenreIds.Should().BeEquivalentTo(new[] { 2, 3 });
            result.Movie.Should().BeNull();
            await _movieRepository.DidNotReceive().AddAsync(Arg.Any<Movie>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CreateAsync_GenreRepositoryReturnsDuplicateGenres_ResultingMovieGenresContainDuplicates()
        {
            // Documents current behaviour: ResolveGenresAsync only de-duplicates the *invalid* id list,
            // not the resolved genres themselves. It trusts whatever the repository returns as-is.
            var genre = new Genre { Id = 1, Name = "Sci-Fi" };
            _genreRepository.GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Genre> { genre, genre });
            var dto = new CreateMovieDto { Title = "Arrival", ReleaseYear = 2016, GenreIds = [1] };

            var result = await _sut.CreateAsync(dto);

            result.Outcome.Should().Be(MovieMutationOutcome.Success);
            result.Movie!.Genres.Should().HaveCount(2);
        }

        [Fact]
        public async Task UpdateAsync_MovieNotFound_ReturnsNotFoundWithoutQueryingGenresOrUpdating()
        {
            _movieRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Movie?)null);
            var dto = new UpdateMovieDto { Title = "Arrival", ReleaseYear = 2016, GenreIds = [1] };

            var result = await _sut.UpdateAsync(1, dto);

            result.Outcome.Should().Be(MovieMutationOutcome.NotFound);
            await _genreRepository.DidNotReceive().GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>());
            await _movieRepository.DidNotReceive().UpdateAsync(Arg.Any<Movie>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateAsync_InvalidGenreIds_ReturnsInvalidGenreIdsWithoutMutatingOrUpdatingMovie()
        {
            var existingGenre = new Genre { Id = 1, Name = "Sci-Fi" };
            var existingMovie = new Movie { Id = 1, Title = "Old Title", ReleaseYear = 2000, Genres = [existingGenre] };
            _movieRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existingMovie);
            _genreRepository.GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>()).Returns(new List<Genre>());
            var dto = new UpdateMovieDto { Title = "New Title", ReleaseYear = 2020, GenreIds = [99] };

            var result = await _sut.UpdateAsync(1, dto);

            result.Outcome.Should().Be(MovieMutationOutcome.InvalidGenreIds);
            result.InvalidGenreIds.Should().BeEquivalentTo(new[] { 99 });
            existingMovie.Title.Should().Be("Old Title");
            existingMovie.Genres.Should().ContainSingle().Which.Should().BeSameAs(existingGenre);
            await _movieRepository.DidNotReceive().UpdateAsync(Arg.Any<Movie>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateAsync_ValidGenreIds_UpdatesFieldsReplacesGenresAndPersists()
        {
            var oldGenre = new Genre { Id = 1, Name = "Sci-Fi" };
            var newGenre = new Genre { Id = 2, Name = "Drama" };
            var existingMovie = new Movie { Id = 1, Title = "Old Title", ReleaseYear = 2000, Genres = [oldGenre] };
            _movieRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existingMovie);
            _genreRepository.GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Genre> { newGenre });
            var dto = new UpdateMovieDto { Title = "New Title", ReleaseYear = 2020, GenreIds = [2] };

            var result = await _sut.UpdateAsync(1, dto);

            result.Outcome.Should().Be(MovieMutationOutcome.Success);
            existingMovie.Title.Should().Be("New Title");
            existingMovie.ReleaseYear.Should().Be(2020);
            existingMovie.Genres.Should().ContainSingle().Which.Should().BeSameAs(newGenre);
            result.Movie!.Genres.Should().ContainSingle(g => g.Name == "Drama");
            await _movieRepository.Received(1).UpdateAsync(existingMovie, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateAsync_ValidWithEmptyGenreIds_ClearsGenres()
        {
            var existingMovie = new Movie { Id = 1, Title = "Old Title", ReleaseYear = 2000, Genres = [new Genre { Id = 1, Name = "Sci-Fi" }] };
            _movieRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existingMovie);
            var dto = new UpdateMovieDto { Title = "New Title", ReleaseYear = 2020, GenreIds = [] };

            var result = await _sut.UpdateAsync(1, dto);

            result.Outcome.Should().Be(MovieMutationOutcome.Success);
            existingMovie.Genres.Should().BeEmpty();
            result.Movie!.Genres.Should().BeEmpty();
        }

        [Fact]
        public async Task DeleteAsync_RepositoryReturnsTrue_ReturnsTrue()
        {
            _movieRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns(true);

            var result = await _sut.DeleteAsync(1);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteAsync_RepositoryReturnsFalse_ReturnsFalse()
        {
            _movieRepository.DeleteAsync(99, Arg.Any<CancellationToken>()).Returns(false);

            var result = await _sut.DeleteAsync(99);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsync_ForwardsIdAndCancellationTokenToRepository()
        {
            using var cts = new CancellationTokenSource();
            _movieRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns(true);

            await _sut.DeleteAsync(1, cts.Token);

            await _movieRepository.Received(1).DeleteAsync(1, cts.Token);
        }
    }
}
