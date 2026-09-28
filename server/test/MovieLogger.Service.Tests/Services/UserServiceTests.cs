using AutoMapper;
using FluentAssertions;
using MovieLogger.Service.Dtos.Users;
using MovieLogger.Service.Entities;
using MovieLogger.Service.Repositories;
using MovieLogger.Service.Security;
using MovieLogger.Service.Services;
using MovieLogger.Service.Tests.TestSupport;
using NSubstitute;

namespace MovieLogger.Service.Tests.Services
{
    public class UserServiceTests
    {
        private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
        private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
        private readonly IMapper _mapper = TestMapperFactory.Create();
        private readonly UserService _sut;

        public UserServiceTests()
        {
            _sut = new UserService(_userRepository, _passwordHasher, _mapper);
        }

        [Fact]
        public async Task GetByIdAsync_UserExists_ReturnsMappedDto()
        {
            var user = new User { Id = 1, DisplayName = "alice", Email = "alice@example.com" };
            _userRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);

            var result = await _sut.GetByIdAsync(1);

            result.Should().NotBeNull();
            result!.DisplayName.Should().Be("alice");
        }

        [Fact]
        public async Task GetByIdAsync_UserDoesNotExist_ReturnsNull()
        {
            _userRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((User?)null);

            var result = await _sut.GetByIdAsync(99);

            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsync_UserNotFound_ReturnsFalseWithoutUpdating()
        {
            _userRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((User?)null);
            var dto = new UpdateUserDto { DisplayName = "new-name", Email = "new@example.com" };

            var result = await _sut.UpdateAsync(99, dto);

            result.Should().BeFalse();
            await _userRepository.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateAsync_UserExists_MutatesEntityAndPersists()
        {
            var existingUser = new User { Id = 1, DisplayName = "old-name", Email = "old@example.com" };
            _userRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existingUser);
            var dto = new UpdateUserDto { DisplayName = "new-name", Email = "new@example.com" };

            var result = await _sut.UpdateAsync(1, dto);

            result.Should().BeTrue();
            existingUser.DisplayName.Should().Be("new-name");
            existingUser.Email.Should().Be("new@example.com");
            existingUser.UpdatedAt.Should().NotBeNull();
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

        [Fact]
        public async Task ChangePasswordAsync_UserNotFound_ReturnsUserNotFound()
        {
            _userRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((User?)null);
            var dto = new ChangePasswordDto { CurrentPassword = "old", NewPassword = "new-password", ConfirmNewPassword = "new-password" };

            var result = await _sut.ChangePasswordAsync(99, dto);

            result.Outcome.Should().Be(ChangePasswordOutcome.UserNotFound);
        }

        [Fact]
        public async Task ChangePasswordAsync_CurrentPasswordIncorrect_ReturnsIncorrectCurrentPasswordWithoutUpdating()
        {
            var user = new User { Id = 1, PasswordHash = "stored-hash" };
            _userRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);
            _passwordHasher.VerifyPassword("wrong", "stored-hash").Returns(false);
            var dto = new ChangePasswordDto { CurrentPassword = "wrong", NewPassword = "new-password", ConfirmNewPassword = "new-password" };

            var result = await _sut.ChangePasswordAsync(1, dto);

            result.Outcome.Should().Be(ChangePasswordOutcome.IncorrectCurrentPassword);
            await _userRepository.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ChangePasswordAsync_CurrentPasswordCorrect_UpdatesHashAndPersists()
        {
            var user = new User { Id = 1, PasswordHash = "old-hash" };
            _userRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);
            _passwordHasher.VerifyPassword("old", "old-hash").Returns(true);
            _passwordHasher.HashPassword("new-password").Returns("new-hash");
            var dto = new ChangePasswordDto { CurrentPassword = "old", NewPassword = "new-password", ConfirmNewPassword = "new-password" };

            var result = await _sut.ChangePasswordAsync(1, dto);

            result.Outcome.Should().Be(ChangePasswordOutcome.Success);
            user.PasswordHash.Should().Be("new-hash");
            await _userRepository.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
        }
    }
}
