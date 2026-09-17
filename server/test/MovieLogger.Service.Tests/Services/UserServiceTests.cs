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
    public class UserServiceTests
    {
        private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
        private readonly IMapper _mapper = TestMapperFactory.Create();
        private readonly UserService _sut;

        public UserServiceTests()
        {
            _sut = new UserService(_userRepository, _mapper);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsUsersMappedToResponseDtos()
        {
            var users = new List<User>
            {
                new() { Id = 1, Username = "alice", Email = "alice@example.com", CreatedAt = new DateTime(2024, 1, 1) },
                new() { Id = 2, Username = "bob", Email = "bob@example.com", CreatedAt = new DateTime(2024, 2, 1) },
            };
            _userRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(users);

            var result = await _sut.GetAllAsync();

            result.Should().HaveCount(2);
            result.Select(u => u.Username).Should().BeEquivalentTo("alice", "bob");
        }

        [Fact]
        public async Task GetAllAsync_RepositoryReturnsEmpty_ReturnsEmptyList()
        {
            _userRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<User>());

            var result = await _sut.GetAllAsync();

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByIdAsync_UserExists_ReturnsMappedDto()
        {
            var user = new User { Id = 1, Username = "alice", Email = "alice@example.com" };
            _userRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);

            var result = await _sut.GetByIdAsync(1);

            result.Should().NotBeNull();
            result!.Username.Should().Be("alice");
        }

        [Fact]
        public async Task GetByIdAsync_UserDoesNotExist_ReturnsNull()
        {
            _userRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((User?)null);

            var result = await _sut.GetByIdAsync(99);

            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateAsync_MapsDtoSetsCreatedAtAddsToRepositoryAndReturnsMappedResponse()
        {
            var dto = new CreateUserDto { Username = "alice", Email = "alice@example.com" };

            var result = await _sut.CreateAsync(dto);

            result.Username.Should().Be("alice");
            result.Email.Should().Be("alice@example.com");
            result.CreatedAt.Should().NotBe(default);
            await _userRepository.Received(1).AddAsync(
                Arg.Is<User>(u => u.Username == "alice" && u.CreatedAt != default),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateAsync_UserNotFound_ReturnsFalseWithoutUpdating()
        {
            _userRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((User?)null);
            var dto = new UpdateUserDto { Username = "new-name", Email = "new@example.com" };

            var result = await _sut.UpdateAsync(99, dto);

            result.Should().BeFalse();
            await _userRepository.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateAsync_UserExists_MutatesEntityAndPersists()
        {
            var existingUser = new User { Id = 1, Username = "old-name", Email = "old@example.com" };
            _userRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existingUser);
            var dto = new UpdateUserDto { Username = "new-name", Email = "new@example.com" };

            var result = await _sut.UpdateAsync(1, dto);

            result.Should().BeTrue();
            existingUser.Username.Should().Be("new-name");
            existingUser.Email.Should().Be("new@example.com");
            await _userRepository.Received(1).UpdateAsync(existingUser, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteAsync_RepositoryReturnsTrue_ReturnsTrue()
        {
            _userRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns(true);

            var result = await _sut.DeleteAsync(1);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteAsync_RepositoryReturnsFalse_ReturnsFalse()
        {
            _userRepository.DeleteAsync(99, Arg.Any<CancellationToken>()).Returns(false);

            var result = await _sut.DeleteAsync(99);

            result.Should().BeFalse();
        }
    }
}
