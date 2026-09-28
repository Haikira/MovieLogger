using AutoMapper;
using FluentAssertions;
using MovieLogger.Service.Dtos.MovieWatches;
using MovieLogger.Service.Entities;
using MovieLogger.Service.Repositories;
using MovieLogger.Service.Services;
using MovieLogger.Service.Tests.TestSupport;
using NSubstitute;

namespace MovieLogger.Service.Tests.Services
{
    public class MovieWatchServiceTests
    {
        private readonly IMovieWatchRepository _movieWatchRepository = Substitute.For<IMovieWatchRepository>();
        private readonly IMovieRepository _movieRepository = Substitute.For<IMovieRepository>();
        private readonly IMapper _mapper = TestMapperFactory.Create();
        private readonly MovieWatchService _sut;

        public MovieWatchServiceTests()
        {
            _sut = new MovieWatchService(_movieWatchRepository, _movieRepository, _mapper);
        }

        [Fact]
        public async Task GetMineAsync_ReturnsOnlyTheGivenUsersWatchesMappedToResponseDtos()
        {
            var watches = new List<MovieWatch>
            {
                new() { Id = 1, UserId = 1, MovieId = 1, DateWatched = new DateTime(2024, 1, 1), Rating = 4 },
                new() { Id = 2, UserId = 1, MovieId = 2, DateWatched = new DateTime(2024, 2, 1), Rating = null },
            };
            _movieWatchRepository.GetByUserIdAsync(1, Arg.Any<CancellationToken>()).Returns(watches);

            var result = await _sut.GetMineAsync(1);

            result.Should().HaveCount(2);
            result[0].Rating.Should().Be(4);
            await _movieWatchRepository.Received(1).GetByUserIdAsync(1, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetByIdAsync_WatchBelongsToRequestingUser_ReturnsMappedDto()
        {
            var watch = new MovieWatch { Id = 1, UserId = 1, MovieId = 1, DateWatched = new DateTime(2024, 1, 1) };
            _movieWatchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(watch);

            var result = await _sut.GetByIdAsync(1, userId: 1);

            result.Should().NotBeNull();
            result!.MovieId.Should().Be(1);
        }

        [Fact]
        public async Task GetByIdAsync_WatchBelongsToAnotherUser_ReturnsNull()
        {
            var watch = new MovieWatch { Id = 1, UserId = 2, MovieId = 1, DateWatched = new DateTime(2024, 1, 1) };
            _movieWatchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(watch);

            var result = await _sut.GetByIdAsync(1, userId: 1);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_WatchDoesNotExist_ReturnsNull()
        {
            _movieWatchRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((MovieWatch?)null);

            var result = await _sut.GetByIdAsync(99, userId: 1);

            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateAsync_MovieExists_LogsMovieForTheGivenUserFromJwt()
        {
            _movieRepository.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(new Movie { Id = 2 });
            var dto = new CreateMovieWatchDto { MovieId = 2, DateWatched = new DateTime(2024, 1, 1), Rating = 4 };

            var result = await _sut.CreateAsync(dto, userId: 1);

            result.Outcome.Should().Be(MovieWatchMutationOutcome.Success);
            result.MovieWatch!.Rating.Should().Be(4);
            await _movieWatchRepository.Received(1).AddAsync(
                Arg.Is<MovieWatch>(w => w.UserId == 1 && w.MovieId == 2),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CreateAsync_SameMovieLoggedTwice_CreatesTwoSeparateRecords()
        {
            _movieRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new Movie { Id = 1 });
            var firstWatch = new CreateMovieWatchDto { MovieId = 1, DateWatched = new DateTime(2021, 2, 2) };
            var secondWatch = new CreateMovieWatchDto { MovieId = 1, DateWatched = new DateTime(2023, 10, 31) };

            await _sut.CreateAsync(firstWatch, userId: 1);
            await _sut.CreateAsync(secondWatch, userId: 1);

            await _movieWatchRepository.Received(2).AddAsync(
                Arg.Is<MovieWatch>(w => w.UserId == 1 && w.MovieId == 1),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CreateAsync_InvalidMovieId_ReturnsInvalidMovieIdWithoutAdding()
        {
            _movieRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Movie?)null);
            var dto = new CreateMovieWatchDto { MovieId = 99, DateWatched = new DateTime(2024, 1, 1) };

            var result = await _sut.CreateAsync(dto, userId: 1);

            result.Outcome.Should().Be(MovieWatchMutationOutcome.InvalidMovieId);
            await _movieWatchRepository.DidNotReceive().AddAsync(Arg.Any<MovieWatch>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateAsync_WatchNotFound_ReturnsFalseWithoutUpdating()
        {
            _movieWatchRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((MovieWatch?)null);
            var dto = new UpdateMovieWatchDto { DateWatched = new DateTime(2024, 1, 1), Rating = 5 };

            var result = await _sut.UpdateAsync(99, dto, userId: 1);

            result.Should().BeFalse();
            await _movieWatchRepository.DidNotReceive().UpdateAsync(Arg.Any<MovieWatch>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateAsync_WatchBelongsToAnotherUser_ReturnsFalseWithoutUpdating()
        {
            var existingWatch = new MovieWatch { Id = 1, UserId = 2, MovieId = 1, DateWatched = new DateTime(2024, 1, 1) };
            _movieWatchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existingWatch);
            var dto = new UpdateMovieWatchDto { DateWatched = new DateTime(2024, 1, 1), Rating = 5 };

            var result = await _sut.UpdateAsync(1, dto, userId: 1);

            result.Should().BeFalse();
            await _movieWatchRepository.DidNotReceive().UpdateAsync(Arg.Any<MovieWatch>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateAsync_WatchBelongsToRequestingUser_UpdatesFieldsAndPersists()
        {
            var existingWatch = new MovieWatch { Id = 1, UserId = 1, MovieId = 1, DateWatched = new DateTime(2024, 1, 1), Rating = 3 };
            _movieWatchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existingWatch);
            var dto = new UpdateMovieWatchDto { DateWatched = new DateTime(2024, 3, 1), Rating = 5, Notes = "Great" };

            var result = await _sut.UpdateAsync(1, dto, userId: 1);

            result.Should().BeTrue();
            existingWatch.Rating.Should().Be(5);
            existingWatch.Notes.Should().Be("Great");
            await _movieWatchRepository.Received(1).UpdateAsync(existingWatch, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteAsync_WatchBelongsToAnotherUser_ReturnsFalseWithoutDeleting()
        {
            var existingWatch = new MovieWatch { Id = 1, UserId = 2, MovieId = 1 };
            _movieWatchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existingWatch);

            var result = await _sut.DeleteAsync(1, userId: 1);

            result.Should().BeFalse();
            await _movieWatchRepository.DidNotReceive().DeleteAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteAsync_WatchBelongsToRequestingUser_DeletesAndReturnsTrue()
        {
            var existingWatch = new MovieWatch { Id = 1, UserId = 1, MovieId = 1 };
            _movieWatchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existingWatch);
            _movieWatchRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns(true);

            var result = await _sut.DeleteAsync(1, userId: 1);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task GetMyMoviesAsync_ForwardsUserIdAndQueryAndReturnsPagedAggregates()
        {
            var movie = new Movie { Id = 1, Title = "Alien", ReleaseYear = 1979, Director = "Ridley Scott", RuntimeMinutes = 117 };
            var aggregate = new MyMovieAggregate(movie, new DateTime(2023, 10, 31), 5, 3);
            var query = new MyMoviesQueryDto { Page = 1, PageSize = 20 };
            _movieWatchRepository.SearchMyMoviesAsync(1, query, Arg.Any<CancellationToken>())
                .Returns((new List<MyMovieAggregate> { aggregate }, 1));

            var result = await _sut.GetMyMoviesAsync(1, query);

            result.TotalCount.Should().Be(1);
            result.Items.Should().ContainSingle();
            result.Items[0].Title.Should().Be("Alien");
            result.Items[0].TimesWatched.Should().Be(3);
            result.Items[0].LastRating.Should().Be(5);
        }

        [Fact]
        public async Task GetHistoryForMovieAsync_ReturnsOnlyRequestingUsersHistoryForThatMovie()
        {
            var history = new List<MovieWatch>
            {
                new() { Id = 1, UserId = 1, MovieId = 1, DateWatched = new DateTime(2021, 2, 2) },
            };
            _movieWatchRepository.GetHistoryForMovieAsync(1, 1, Arg.Any<CancellationToken>()).Returns(history);

            var result = await _sut.GetHistoryForMovieAsync(movieId: 1, userId: 1);

            result.Should().ContainSingle();
            await _movieWatchRepository.Received(1).GetHistoryForMovieAsync(1, 1, Arg.Any<CancellationToken>());
        }
    }
}
