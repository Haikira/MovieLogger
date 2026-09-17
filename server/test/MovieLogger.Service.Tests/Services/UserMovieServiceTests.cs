using AutoMapper;
using FluentAssertions;
using MovieLogger.Service.Dtos.Users;
using MovieLogger.Service.Entities;
using MovieLogger.Service.Repositories;
using MovieLogger.Service.Services;
using MovieLogger.Service.Tests.TestSupport;
using NSubstitute;

namespace MovieLogger.Service.Tests.Services
{
    public class UserMovieServiceTests
    {
        private readonly IUserMovieRepository _userMovieRepository = Substitute.For<IUserMovieRepository>();
        private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
        private readonly IMovieRepository _movieRepository = Substitute.For<IMovieRepository>();
        private readonly IMapper _mapper = TestMapperFactory.Create();
        private readonly UserMovieService _sut;

        public UserMovieServiceTests()
        {
            _sut = new UserMovieService(_userMovieRepository, _userRepository, _movieRepository, _mapper);
        }

        [Fact]
        public async Task GetByUserIdAsync_UserNotFound_ReturnsNull()
        {
            _userRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((User?)null);

            var result = await _sut.GetByUserIdAsync(99);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByUserIdAsync_UserExists_ReturnsMappedList()
        {
            _userRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new User { Id = 1 });
            var movie = new Movie { Id = 1, Title = "Arrival" };
            _userMovieRepository.GetByUserIdAsync(1, Arg.Any<CancellationToken>())
                .Returns(new List<UserMovie> { new() { UserId = 1, MovieId = 1, Movie = movie, IsFavourite = true } });

            var result = await _sut.GetByUserIdAsync(1);

            result.Should().NotBeNull();
            result!.Should().ContainSingle(um => um.Movie.Title == "Arrival" && um.IsFavourite);
        }

        [Fact]
        public async Task SetStatusAsync_UserNotFound_ReturnsUserNotFound()
        {
            _userRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((User?)null);
            var dto = new SetUserMovieStatusDto { IsFavourite = true };

            var result = await _sut.SetStatusAsync(99, 1, dto);

            result.Outcome.Should().Be(SetUserMovieStatusOutcome.UserNotFound);
            await _userMovieRepository.DidNotReceive().UpsertAsync(Arg.Any<UserMovie>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task SetStatusAsync_MovieNotFound_ReturnsMovieNotFound()
        {
            _userRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new User { Id = 1 });
            _movieRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Movie?)null);
            var dto = new SetUserMovieStatusDto { IsFavourite = true };

            var result = await _sut.SetStatusAsync(1, 99, dto);

            result.Outcome.Should().Be(SetUserMovieStatusOutcome.MovieNotFound);
            await _userMovieRepository.DidNotReceive().UpsertAsync(Arg.Any<UserMovie>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task SetStatusAsync_ValidPair_UpsertsAndReturnsSuccess()
        {
            _userRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new User { Id = 1 });
            _movieRepository.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(new Movie { Id = 2 });
            var dto = new SetUserMovieStatusDto { IsFavourite = true, IsOwned = true };

            var result = await _sut.SetStatusAsync(1, 2, dto);

            result.Outcome.Should().Be(SetUserMovieStatusOutcome.Success);
            await _userMovieRepository.Received(1).UpsertAsync(
                Arg.Is<UserMovie>(um => um.UserId == 1 && um.MovieId == 2 && um.IsFavourite && um.IsOwned),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task RemoveAsync_RepositoryReturnsTrue_ReturnsTrue()
        {
            _userMovieRepository.DeleteAsync(1, 2, Arg.Any<CancellationToken>()).Returns(true);

            var result = await _sut.RemoveAsync(1, 2);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task RemoveAsync_RepositoryReturnsFalse_ReturnsFalse()
        {
            _userMovieRepository.DeleteAsync(1, 99, Arg.Any<CancellationToken>()).Returns(false);

            var result = await _sut.RemoveAsync(1, 99);

            result.Should().BeFalse();
        }
    }
}
