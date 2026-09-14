using AutoMapper;
using FluentAssertions;
using MovieLogger.Service.Dtos.Lists;
using MovieLogger.Service.Entities;
using MovieLogger.Service.Repositories;
using MovieLogger.Service.Services;
using MovieLogger.Service.Tests.TestSupport;
using NSubstitute;

namespace MovieLogger.Service.Tests.Services
{
    public class MovieListServiceTests
    {
        private readonly IMovieListRepository _movieListRepository = Substitute.For<IMovieListRepository>();
        private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
        private readonly IMapper _mapper = TestMapperFactory.Create();
        private readonly MovieListService _sut;

        public MovieListServiceTests()
        {
            _sut = new MovieListService(_movieListRepository, _userRepository, _mapper);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsListsMappedToResponseDtosWithMovies()
        {
            var movie = new Movie { Id = 1, Title = "Arrival" };
            var lists = new List<MovieList>
            {
                new()
                {
                    Id = 1,
                    UserId = 1,
                    Name = "Favourites",
                    ListMovies = [new ListMovie { ListId = 1, MovieId = 1, Movie = movie, AddedAt = new DateTime(2024, 1, 1) }]
                },
            };
            _movieListRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(lists);

            var result = await _sut.GetAllAsync();

            result.Should().HaveCount(1);
            result[0].Movies.Should().ContainSingle(m => m.Title == "Arrival");
        }

        [Fact]
        public async Task GetAllAsync_RepositoryReturnsEmpty_ReturnsEmptyList()
        {
            _movieListRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<MovieList>());

            var result = await _sut.GetAllAsync();

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByIdAsync_ListExists_ReturnsMappedDto()
        {
            var list = new MovieList { Id = 1, UserId = 1, Name = "Favourites" };
            _movieListRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(list);

            var result = await _sut.GetByIdAsync(1);

            result.Should().NotBeNull();
            result!.Name.Should().Be("Favourites");
        }

        [Fact]
        public async Task GetByIdAsync_ListDoesNotExist_ReturnsNull()
        {
            _movieListRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((MovieList?)null);

            var result = await _sut.GetByIdAsync(99);

            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateAsync_ValidUserId_SetsCreatedAtAddsAndReturnsSuccess()
        {
            _userRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new User { Id = 1 });
            var dto = new CreateMovieListDto { UserId = 1, Name = "Favourites" };

            var result = await _sut.CreateAsync(dto);

            result.Outcome.Should().Be(MovieListMutationOutcome.Success);
            result.MovieList!.Name.Should().Be("Favourites");
            await _movieListRepository.Received(1).AddAsync(
                Arg.Is<MovieList>(l => l.Name == "Favourites" && l.CreatedAt != default),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CreateAsync_InvalidUserId_ReturnsInvalidUserIdWithoutAdding()
        {
            _userRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((User?)null);
            var dto = new CreateMovieListDto { UserId = 99, Name = "Favourites" };

            var result = await _sut.CreateAsync(dto);

            result.Outcome.Should().Be(MovieListMutationOutcome.InvalidUserId);
            await _movieListRepository.DidNotReceive().AddAsync(Arg.Any<MovieList>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateAsync_ListNotFound_ReturnsFalseWithoutUpdating()
        {
            _movieListRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((MovieList?)null);
            var dto = new UpdateMovieListDto { Name = "New Name" };

            var result = await _sut.UpdateAsync(99, dto);

            result.Should().BeFalse();
            await _movieListRepository.DidNotReceive().UpdateAsync(Arg.Any<MovieList>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateAsync_ListExists_UpdatesNameDescriptionAndPersists()
        {
            var existingList = new MovieList { Id = 1, UserId = 1, Name = "Old Name" };
            _movieListRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existingList);
            var dto = new UpdateMovieListDto { Name = "New Name", Description = "Updated" };

            var result = await _sut.UpdateAsync(1, dto);

            result.Should().BeTrue();
            existingList.Name.Should().Be("New Name");
            existingList.Description.Should().Be("Updated");
            await _movieListRepository.Received(1).UpdateAsync(existingList, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteAsync_RepositoryReturnsTrue_ReturnsTrue()
        {
            _movieListRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns(true);

            var result = await _sut.DeleteAsync(1);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteAsync_RepositoryReturnsFalse_ReturnsFalse()
        {
            _movieListRepository.DeleteAsync(99, Arg.Any<CancellationToken>()).Returns(false);

            var result = await _sut.DeleteAsync(99);

            result.Should().BeFalse();
        }
    }
}
