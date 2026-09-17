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
        private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
        private readonly IMovieRepository _movieRepository = Substitute.For<IMovieRepository>();
        private readonly IMapper _mapper = TestMapperFactory.Create();
        private readonly MovieWatchService _sut;

        public MovieWatchServiceTests()
        {
            _sut = new MovieWatchService(_movieWatchRepository, _userRepository, _movieRepository, _mapper);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsWatchesMappedToResponseDtos()
        {
            var watches = new List<MovieWatch>
            {
                new() { Id = 1, UserId = 1, MovieId = 1, WatchedAt = new DateTime(2024, 1, 1), Score = 8.5m },
                new() { Id = 2, UserId = 1, MovieId = 2, WatchedAt = new DateTime(2024, 2, 1), Score = null },
            };
            _movieWatchRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(watches);

            var result = await _sut.GetAllAsync();

            result.Should().HaveCount(2);
            result[0].Score.Should().Be(8.5m);
        }

        [Fact]
        public async Task GetAllAsync_RepositoryReturnsEmpty_ReturnsEmptyList()
        {
            _movieWatchRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<MovieWatch>());

            var result = await _sut.GetAllAsync();

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByIdAsync_WatchExists_ReturnsMappedDto()
        {
            var watch = new MovieWatch { Id = 1, UserId = 1, MovieId = 1, WatchedAt = new DateTime(2024, 1, 1) };
            _movieWatchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(watch);

            var result = await _sut.GetByIdAsync(1);

            result.Should().NotBeNull();
            result!.MovieId.Should().Be(1);
        }

        [Fact]
        public async Task GetByIdAsync_WatchDoesNotExist_ReturnsNull()
        {
            _movieWatchRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((MovieWatch?)null);

            var result = await _sut.GetByIdAsync(99);

            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateAsync_UserAndMovieValid_AddsAndReturnsSuccess()
        {
            _userRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new User { Id = 1 });
            _movieRepository.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(new Movie { Id = 2 });
            var dto = new CreateMovieWatchDto { UserId = 1, MovieId = 2, WatchedAt = new DateTime(2024, 1, 1), Score = 7m };

            var result = await _sut.CreateAsync(dto);

            result.Outcome.Should().Be(MovieWatchMutationOutcome.Success);
            result.MovieWatch!.Score.Should().Be(7m);
            await _movieWatchRepository.Received(1).AddAsync(
                Arg.Is<MovieWatch>(w => w.UserId == 1 && w.MovieId == 2),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CreateAsync_InvalidUserId_ReturnsInvalidUserIdWithoutAdding()
        {
            _userRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((User?)null);
            var dto = new CreateMovieWatchDto { UserId = 99, MovieId = 1, WatchedAt = new DateTime(2024, 1, 1) };

            var result = await _sut.CreateAsync(dto);

            result.Outcome.Should().Be(MovieWatchMutationOutcome.InvalidUserId);
            await _movieWatchRepository.DidNotReceive().AddAsync(Arg.Any<MovieWatch>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CreateAsync_InvalidMovieId_ReturnsInvalidMovieIdWithoutAdding()
        {
            _userRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new User { Id = 1 });
            _movieRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Movie?)null);
            var dto = new CreateMovieWatchDto { UserId = 1, MovieId = 99, WatchedAt = new DateTime(2024, 1, 1) };

            var result = await _sut.CreateAsync(dto);

            result.Outcome.Should().Be(MovieWatchMutationOutcome.InvalidMovieId);
            await _movieWatchRepository.DidNotReceive().AddAsync(Arg.Any<MovieWatch>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateAsync_WatchNotFound_ReturnsFalseWithoutUpdating()
        {
            _movieWatchRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((MovieWatch?)null);
            var dto = new UpdateMovieWatchDto { WatchedAt = new DateTime(2024, 1, 1), Score = 5m };

            var result = await _sut.UpdateAsync(99, dto);

            result.Should().BeFalse();
            await _movieWatchRepository.DidNotReceive().UpdateAsync(Arg.Any<MovieWatch>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateAsync_WatchExists_UpdatesWatchedAtScoreReviewAndPersists()
        {
            var existingWatch = new MovieWatch { Id = 1, UserId = 1, MovieId = 1, WatchedAt = new DateTime(2024, 1, 1), Score = 5m };
            _movieWatchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existingWatch);
            var dto = new UpdateMovieWatchDto { WatchedAt = new DateTime(2024, 3, 1), Score = 9m, Review = "Great" };

            var result = await _sut.UpdateAsync(1, dto);

            result.Should().BeTrue();
            existingWatch.Score.Should().Be(9m);
            existingWatch.Review.Should().Be("Great");
            await _movieWatchRepository.Received(1).UpdateAsync(existingWatch, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteAsync_RepositoryReturnsTrue_ReturnsTrue()
        {
            _movieWatchRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns(true);

            var result = await _sut.DeleteAsync(1);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteAsync_RepositoryReturnsFalse_ReturnsFalse()
        {
            _movieWatchRepository.DeleteAsync(99, Arg.Any<CancellationToken>()).Returns(false);

            var result = await _sut.DeleteAsync(99);

            result.Should().BeFalse();
        }
    }
}
