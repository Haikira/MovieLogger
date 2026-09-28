using AutoMapper;
using FluentAssertions;
using MovieLogger.Service.Dtos.MovieWatches;
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
        private readonly IMovieWatchRepository _movieWatchRepository = Substitute.For<IMovieWatchRepository>();
        private readonly IMapper _mapper = TestMapperFactory.Create();
        private readonly MovieService _sut;

        public MovieServiceTests()
        {
            _sut = new MovieService(_movieRepository, _genreRepository, _movieWatchRepository, _mapper);
        }

        [Fact]
        public async Task SearchAsync_ReturnsMoviesMappedToResponseDtosWithPagingInfo()
        {
            var movies = new List<Movie>
            {
                new() { Id = 1, Title = "The Matrix", ReleaseYear = 1999, Genres = [new Genre { Id = 1, Name = "Sci-Fi" }] },
                new() { Id = 2, Title = "Heat", ReleaseYear = 1995, Genres = [] },
            };
            _movieRepository.SearchAsync(Arg.Any<MovieSearchQueryDto>(), Arg.Any<CancellationToken>())
                .Returns((movies, 2));
            var query = new MovieSearchQueryDto { Page = 1, PageSize = 20 };

            var result = await _sut.SearchAsync(query);

            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
            result.Page.Should().Be(1);
            result.PageSize.Should().Be(20);
            result.Items[0].Title.Should().Be("The Matrix");
            result.Items[0].Genres.Should().ContainSingle(g => g.Name == "Sci-Fi");
        }

        [Fact]
        public async Task SearchAsync_RepositoryReturnsEmpty_ReturnsEmptyItems()
        {
            _movieRepository.SearchAsync(Arg.Any<MovieSearchQueryDto>(), Arg.Any<CancellationToken>())
                .Returns((new List<Movie>(), 0));

            var result = await _sut.SearchAsync(new MovieSearchQueryDto());

            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task GetDetailsAsync_MovieExistsNoCurrentUser_ReturnsMovieWithoutUserHistory()
        {
            var movie = new Movie { Id = 1, Title = "The Matrix", ReleaseYear = 1999 };
            _movieRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(movie);

            var result = await _sut.GetDetailsAsync(1, currentUserId: null);

            result.Should().NotBeNull();
            result!.Movie.Title.Should().Be("The Matrix");
            result.UserHistory.Should().BeNull();
            await _movieWatchRepository.DidNotReceive().GetHistoryForMovieAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetDetailsAsync_MovieExistsWithCurrentUser_ReturnsUserHistorySummary()
        {
            var movie = new Movie { Id = 1, Title = "Alien", ReleaseYear = 1979 };
            _movieRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(movie);
            var history = new List<MovieWatch>
            {
                new() { Id = 1, UserId = 5, MovieId = 1, DateWatched = new DateTime(2021, 2, 2), Rating = 4 },
                new() { Id = 2, UserId = 5, MovieId = 1, DateWatched = new DateTime(2023, 10, 31), Rating = 5 },
            };
            _movieWatchRepository.GetHistoryForMovieAsync(5, 1, Arg.Any<CancellationToken>()).Returns(history);

            var result = await _sut.GetDetailsAsync(1, currentUserId: 5);

            result!.UserHistory.Should().NotBeNull();
            result.UserHistory!.TimesWatched.Should().Be(2);
            result.UserHistory.LastWatchedAt.Should().Be(new DateTime(2023, 10, 31));
            result.UserHistory.LastRating.Should().Be(5);
            result.UserHistory.Logs.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetDetailsAsync_MovieDoesNotExist_ReturnsNull()
        {
            _movieRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Movie?)null);

            var result = await _sut.GetDetailsAsync(99, currentUserId: null);

            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateAsync_NoGenreIds_AddsMovieToCatalogueWithCreatedByUserId()
        {
            var dto = new CreateMovieDto { Title = "Arrival", ReleaseYear = 2016, GenreIds = [] };

            var result = await _sut.CreateAsync(dto, createdByUserId: 7);

            await _genreRepository.DidNotReceive().GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>());
            result.Outcome.Should().Be(MovieMutationOutcome.Success);
            result.Movie!.Genres.Should().BeEmpty();
            await _movieRepository.Received(1).AddAsync(
                Arg.Is<Movie>(m => m.Title == "Arrival" && m.CreatedByUserId == 7),
                Arg.Any<CancellationToken>());
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

            var result = await _sut.CreateAsync(dto, createdByUserId: 7);

            result.Outcome.Should().Be(MovieMutationOutcome.Success);
            result.Movie!.Genres.Should().HaveCount(2);
            result.Movie.Genres.Select(g => g.Name).Should().BeEquivalentTo("Sci-Fi", "Drama");
        }

        [Fact]
        public async Task CreateAsync_SomeGenreIdsInvalid_ReturnsInvalidGenreIdsAndDoesNotAddMovie()
        {
            var genres = new List<Genre> { new() { Id = 1, Name = "Sci-Fi" } };
            _genreRepository.GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>()).Returns(genres);
            var dto = new CreateMovieDto { Title = "Arrival", ReleaseYear = 2016, GenreIds = [1, 2, 3] };

            var result = await _sut.CreateAsync(dto, createdByUserId: 7);

            result.Outcome.Should().Be(MovieMutationOutcome.InvalidGenreIds);
            result.InvalidGenreIds.Should().BeEquivalentTo(new[] { 2, 3 });
            result.Movie.Should().BeNull();
            await _movieRepository.DidNotReceive().AddAsync(Arg.Any<Movie>(), Arg.Any<CancellationToken>());
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
    }
}
