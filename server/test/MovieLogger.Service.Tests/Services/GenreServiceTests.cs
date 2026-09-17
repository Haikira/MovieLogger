using AutoMapper;
using FluentAssertions;
using MovieLogger.Service.Dtos.Genres;
using MovieLogger.Service.Entities;
using MovieLogger.Service.Repositories;
using MovieLogger.Service.Services;
using MovieLogger.Service.Tests.TestSupport;
using NSubstitute;

namespace MovieLogger.Service.Tests.Services
{
    public class GenreServiceTests
    {
        private readonly IGenreRepository _genreRepository = Substitute.For<IGenreRepository>();
        private readonly IMapper _mapper = TestMapperFactory.Create();
        private readonly GenreService _sut;

        public GenreServiceTests()
        {
            _sut = new GenreService(_genreRepository, _mapper);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsGenresMappedToResponseDtos()
        {
            var genres = new List<Genre>
            {
                new() { Id = 1, Name = "Sci-Fi" },
                new() { Id = 2, Name = "Drama" },
            };
            _genreRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(genres);

            var result = await _sut.GetAllAsync();

            result.Should().HaveCount(2);
            result.Select(g => g.Name).Should().BeEquivalentTo("Sci-Fi", "Drama");
        }

        [Fact]
        public async Task GetAllAsync_RepositoryReturnsEmpty_ReturnsEmptyList()
        {
            _genreRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Genre>());

            var result = await _sut.GetAllAsync();

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByIdAsync_GenreExists_ReturnsMappedDto()
        {
            var genre = new Genre { Id = 1, Name = "Sci-Fi" };
            _genreRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(genre);

            var result = await _sut.GetByIdAsync(1);

            result.Should().NotBeNull();
            result!.Name.Should().Be("Sci-Fi");
        }

        [Fact]
        public async Task GetByIdAsync_GenreDoesNotExist_ReturnsNull()
        {
            _genreRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Genre?)null);

            var result = await _sut.GetByIdAsync(99);

            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateAsync_MapsDtoAddsToRepositoryAndReturnsMappedResponse()
        {
            var dto = new CreateGenreDto { Name = "Sci-Fi" };

            var result = await _sut.CreateAsync(dto);

            result.Name.Should().Be("Sci-Fi");
            await _genreRepository.Received(1).AddAsync(
                Arg.Is<Genre>(g => g.Name == "Sci-Fi"),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateAsync_GenreNotFound_ReturnsFalseWithoutUpdating()
        {
            _genreRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Genre?)null);
            var dto = new UpdateGenreDto { Name = "New Name" };

            var result = await _sut.UpdateAsync(99, dto);

            result.Should().BeFalse();
            await _genreRepository.DidNotReceive().UpdateAsync(Arg.Any<Genre>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateAsync_GenreExists_MutatesEntityAndPersists()
        {
            var existingGenre = new Genre { Id = 1, Name = "Old Name" };
            _genreRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existingGenre);
            var dto = new UpdateGenreDto { Name = "New Name" };

            var result = await _sut.UpdateAsync(1, dto);

            result.Should().BeTrue();
            existingGenre.Name.Should().Be("New Name");
            await _genreRepository.Received(1).UpdateAsync(existingGenre, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteAsync_RepositoryReturnsTrue_ReturnsTrue()
        {
            _genreRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns(true);

            var result = await _sut.DeleteAsync(1);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteAsync_RepositoryReturnsFalse_ReturnsFalse()
        {
            _genreRepository.DeleteAsync(99, Arg.Any<CancellationToken>()).Returns(false);

            var result = await _sut.DeleteAsync(99);

            result.Should().BeFalse();
        }
    }
}
