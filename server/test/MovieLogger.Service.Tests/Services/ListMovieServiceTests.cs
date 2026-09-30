using FluentAssertions;
using MovieLogger.Service.Entities;
using MovieLogger.Service.Repositories;
using MovieLogger.Service.Services;
using NSubstitute;

namespace MovieLogger.Service.Tests.Services
{
    public class ListMovieServiceTests
    {
        private readonly IListMovieRepository _listMovieRepository = Substitute.For<IListMovieRepository>();
        private readonly IMovieListRepository _movieListRepository = Substitute.For<IMovieListRepository>();
        private readonly IMovieRepository _movieRepository = Substitute.For<IMovieRepository>();
        private readonly ListMovieService _sut;

        public ListMovieServiceTests()
        {
            _sut = new ListMovieService(_listMovieRepository, _movieListRepository, _movieRepository);
        }

        [Fact]
        public async Task AddAsync_ListNotFound_ReturnsListNotFound()
        {
            _movieListRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((MovieList?)null);

            var result = await _sut.AddAsync(99, 1, userId: 1);

            result.Outcome.Should().Be(AddMovieToListOutcome.ListNotFound);
            await _listMovieRepository.DidNotReceive().AddAsync(Arg.Any<ListMovie>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddAsync_ListOwnedByAnotherUser_ReturnsListNotFoundWithoutAdding()
        {
            _movieListRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new MovieList { Id = 1, UserId = 2 });
            _movieRepository.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(new Movie { Id = 2 });

            var result = await _sut.AddAsync(1, 2, userId: 1);

            result.Outcome.Should().Be(AddMovieToListOutcome.ListNotFound);
            await _listMovieRepository.DidNotReceive().AddAsync(Arg.Any<ListMovie>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddAsync_MovieNotFound_ReturnsMovieNotFound()
        {
            _movieListRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new MovieList { Id = 1, UserId = 1 });
            _movieRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Movie?)null);

            var result = await _sut.AddAsync(1, 99, userId: 1);

            result.Outcome.Should().Be(AddMovieToListOutcome.MovieNotFound);
            await _listMovieRepository.DidNotReceive().AddAsync(Arg.Any<ListMovie>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddAsync_ValidPairNotYetInList_SetsAddedAtAndAddsEntry()
        {
            _movieListRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new MovieList { Id = 1, UserId = 1 });
            _movieRepository.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(new Movie { Id = 2 });
            _listMovieRepository.GetAsync(1, 2, Arg.Any<CancellationToken>()).Returns((ListMovie?)null);

            var result = await _sut.AddAsync(1, 2, userId: 1);

            result.Outcome.Should().Be(AddMovieToListOutcome.Success);
            await _listMovieRepository.Received(1).AddAsync(
                Arg.Is<ListMovie>(lm => lm.ListId == 1 && lm.MovieId == 2 && lm.AddedAt != default),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddAsync_AlreadyInList_IsIdempotentDoesNotDuplicate()
        {
            _movieListRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new MovieList { Id = 1, UserId = 1 });
            _movieRepository.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(new Movie { Id = 2 });
            _listMovieRepository.GetAsync(1, 2, Arg.Any<CancellationToken>())
                .Returns(new ListMovie { ListId = 1, MovieId = 2, AddedAt = new DateTime(2024, 1, 1) });

            var result = await _sut.AddAsync(1, 2, userId: 1);

            result.Outcome.Should().Be(AddMovieToListOutcome.Success);
            await _listMovieRepository.DidNotReceive().AddAsync(Arg.Any<ListMovie>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task RemoveAsync_ListOwnedByUserAndRepositoryReturnsTrue_ReturnsTrue()
        {
            _movieListRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new MovieList { Id = 1, UserId = 1 });
            _listMovieRepository.DeleteAsync(1, 2, Arg.Any<CancellationToken>()).Returns(true);

            var result = await _sut.RemoveAsync(1, 2, userId: 1);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task RemoveAsync_ListOwnedByUserAndRepositoryReturnsFalse_ReturnsFalse()
        {
            _movieListRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new MovieList { Id = 1, UserId = 1 });
            _listMovieRepository.DeleteAsync(1, 99, Arg.Any<CancellationToken>()).Returns(false);

            var result = await _sut.RemoveAsync(1, 99, userId: 1);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task RemoveAsync_ListOwnedByAnotherUser_ReturnsFalseWithoutDeleting()
        {
            _movieListRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new MovieList { Id = 1, UserId = 2 });

            var result = await _sut.RemoveAsync(1, 2, userId: 1);

            result.Should().BeFalse();
            await _listMovieRepository.DidNotReceive().DeleteAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task RemoveAsync_ListNotFound_ReturnsFalseWithoutDeleting()
        {
            _movieListRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((MovieList?)null);

            var result = await _sut.RemoveAsync(99, 2, userId: 1);

            result.Should().BeFalse();
            await _listMovieRepository.DidNotReceive().DeleteAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        }
    }
}
