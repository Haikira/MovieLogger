using AutoMapper;
using FluentAssertions;
using MovieLogger.Service.Entities;
using MovieLogger.Service.Repositories;
using MovieLogger.Service.Services;
using MovieLogger.Service.Tests.TestSupport;
using NSubstitute;

namespace MovieLogger.Service.Tests.Services
{
    public class WatchlistServiceTests
    {
        private readonly IWatchlistItemRepository _watchlistItemRepository = Substitute.For<IWatchlistItemRepository>();
        private readonly IMovieRepository _movieRepository = Substitute.For<IMovieRepository>();
        private readonly IMapper _mapper = TestMapperFactory.Create();
        private readonly WatchlistService _sut;

        public WatchlistServiceTests()
        {
            _sut = new WatchlistService(_watchlistItemRepository, _movieRepository, _mapper);
        }

        [Fact]
        public async Task AddAsync_MovieExistsAndNotAlreadyOnWatchlist_AddsAndReturnsSuccess()
        {
            _movieRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new Movie { Id = 1, Title = "Alien", ReleaseYear = 1979 });
            _watchlistItemRepository.GetAsync(1, 1, Arg.Any<CancellationToken>()).Returns((WatchlistItem?)null);

            var result = await _sut.AddAsync(userId: 1, movieId: 1);

            result.Outcome.Should().Be(AddToWatchlistOutcome.Success);
            result.WatchlistItem!.Title.Should().Be("Alien");
            await _watchlistItemRepository.Received(1).AddAsync(
                Arg.Is<WatchlistItem>(w => w.UserId == 1 && w.MovieId == 1),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddAsync_MovieDoesNotExist_ReturnsMovieNotFoundWithoutAdding()
        {
            _movieRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Movie?)null);

            var result = await _sut.AddAsync(userId: 1, movieId: 99);

            result.Outcome.Should().Be(AddToWatchlistOutcome.MovieNotFound);
            await _watchlistItemRepository.DidNotReceive().AddAsync(Arg.Any<WatchlistItem>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddAsync_AlreadyOnWatchlist_ReturnsAlreadyExistsWithoutAddingDuplicate()
        {
            _movieRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new Movie { Id = 1 });
            _watchlistItemRepository.GetAsync(1, 1, Arg.Any<CancellationToken>())
                .Returns(new WatchlistItem { Id = 5, UserId = 1, MovieId = 1 });

            var result = await _sut.AddAsync(userId: 1, movieId: 1);

            result.Outcome.Should().Be(AddToWatchlistOutcome.AlreadyExists);
            await _watchlistItemRepository.DidNotReceive().AddAsync(Arg.Any<WatchlistItem>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetMineAsync_OnlyQueriesWatchlistForTheGivenUserId()
        {
            var items = new List<WatchlistItem>
            {
                new() { Id = 1, UserId = 1, MovieId = 1, Movie = new Movie { Id = 1, Title = "Alien", ReleaseYear = 1979 } },
            };
            _watchlistItemRepository.GetByUserIdAsync(1, Arg.Any<CancellationToken>()).Returns(items);

            var result = await _sut.GetMineAsync(1);

            result.Should().ContainSingle(i => i.Title == "Alien");
            await _watchlistItemRepository.Received(1).GetByUserIdAsync(1, Arg.Any<CancellationToken>());
            await _watchlistItemRepository.DidNotReceive().GetByUserIdAsync(2, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetMineAsync_MapsMovieDetailsAndGenres()
        {
            var movie = new Movie
            {
                Id = 1,
                Title = "Past Lives",
                ReleaseYear = 2023,
                Director = "Celine Song",
                RuntimeMinutes = 106,
                PosterImageUrl = "https://example.com/past-lives.jpg",
                Genres = [new Genre { Id = 7, Name = "Drama" }, new Genre { Id = 14, Name = "Romance" }]
            };
            var dateAdded = new DateTime(2026, 9, 21, 10, 0, 0, DateTimeKind.Utc);
            _watchlistItemRepository.GetByUserIdAsync(1, Arg.Any<CancellationToken>())
                .Returns([new WatchlistItem { Id = 3, UserId = 1, MovieId = 1, Movie = movie, DateAdded = dateAdded }]);

            var item = (await _sut.GetMineAsync(1)).Should().ContainSingle().Subject;

            item.Id.Should().Be(3);
            item.MovieId.Should().Be(1);
            item.Title.Should().Be("Past Lives");
            item.ReleaseYear.Should().Be(2023);
            item.Director.Should().Be("Celine Song");
            item.RuntimeMinutes.Should().Be(106);
            item.PosterImageUrl.Should().Be("https://example.com/past-lives.jpg");
            item.DateAdded.Should().Be(dateAdded);
            item.Genres.Select(g => (g.Id, g.Name)).Should().BeEquivalentTo([(7, "Drama"), (14, "Romance")]);
        }

        [Fact]
        public async Task RemoveAsync_DelegatesToRepositoryWithUserAndMovieId()
        {
            _watchlistItemRepository.DeleteAsync(1, 2, Arg.Any<CancellationToken>()).Returns(true);

            var result = await _sut.RemoveAsync(1, 2);

            result.Should().BeTrue();
        }
    }
}
