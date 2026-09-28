using AutoMapper;
using FluentAssertions;
using MovieLogger.Service.Dtos.Auth;
using MovieLogger.Service.Entities;
using MovieLogger.Service.Repositories;
using MovieLogger.Service.Security;
using MovieLogger.Service.Services;
using MovieLogger.Service.Tests.TestSupport;
using NSubstitute;

namespace MovieLogger.Service.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
        private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
        private readonly IJwtTokenGenerator _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        private readonly IMapper _mapper = TestMapperFactory.Create();
        private readonly AuthService _sut;

        public AuthServiceTests()
        {
            _sut = new AuthService(_userRepository, _passwordHasher, _jwtTokenGenerator, _mapper);
            _jwtTokenGenerator.GenerateToken(Arg.Any<User>()).Returns(("token", DateTime.UtcNow.AddHours(1)));
        }

        [Fact]
        public async Task RegisterAsync_EmailNotTaken_HashesPasswordAddsUserAndReturnsAuthResponse()
        {
            _userRepository.GetByEmailAsync("alice@example.com", Arg.Any<CancellationToken>()).Returns((User?)null);
            _passwordHasher.HashPassword("Password123").Returns("hashed-password");
            var dto = new RegisterRequestDto
            {
                DisplayName = "alice",
                Email = "alice@example.com",
                Password = "Password123",
                ConfirmPassword = "Password123"
            };

            var result = await _sut.RegisterAsync(dto);

            result.Outcome.Should().Be(RegisterOutcome.Success);
            result.AuthResponse!.Token.Should().Be("token");
            result.AuthResponse.User.DisplayName.Should().Be("alice");
            await _userRepository.Received(1).AddAsync(
                Arg.Is<User>(u => u.Email == "alice@example.com" && u.PasswordHash == "hashed-password"),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task RegisterAsync_EmailAlreadyExists_ReturnsDuplicateEmailWithoutAdding()
        {
            _userRepository.GetByEmailAsync("alice@example.com", Arg.Any<CancellationToken>())
                .Returns(new User { Id = 1, Email = "alice@example.com" });
            var dto = new RegisterRequestDto
            {
                DisplayName = "alice",
                Email = "alice@example.com",
                Password = "Password123",
                ConfirmPassword = "Password123"
            };

            var result = await _sut.RegisterAsync(dto);

            result.Outcome.Should().Be(RegisterOutcome.DuplicateEmail);
            await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsAuthResponse()
        {
            var user = new User { Id = 1, Email = "alice@example.com", PasswordHash = "hashed-password", DisplayName = "alice" };
            _userRepository.GetByEmailAsync("alice@example.com", Arg.Any<CancellationToken>()).Returns(user);
            _passwordHasher.VerifyPassword("Password123", "hashed-password").Returns(true);
            var dto = new LoginRequestDto { Email = "alice@example.com", Password = "Password123" };

            var result = await _sut.LoginAsync(dto);

            result.Outcome.Should().Be(LoginOutcome.Success);
            result.AuthResponse!.User.Email.Should().Be("alice@example.com");
        }

        [Fact]
        public async Task LoginAsync_UnknownEmail_ReturnsInvalidCredentials()
        {
            _userRepository.GetByEmailAsync("nobody@example.com", Arg.Any<CancellationToken>()).Returns((User?)null);
            var dto = new LoginRequestDto { Email = "nobody@example.com", Password = "Password123" };

            var result = await _sut.LoginAsync(dto);

            result.Outcome.Should().Be(LoginOutcome.InvalidCredentials);
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_ReturnsInvalidCredentials()
        {
            var user = new User { Id = 1, Email = "alice@example.com", PasswordHash = "hashed-password" };
            _userRepository.GetByEmailAsync("alice@example.com", Arg.Any<CancellationToken>()).Returns(user);
            _passwordHasher.VerifyPassword("wrong-password", "hashed-password").Returns(false);
            var dto = new LoginRequestDto { Email = "alice@example.com", Password = "wrong-password" };

            var result = await _sut.LoginAsync(dto);

            result.Outcome.Should().Be(LoginOutcome.InvalidCredentials);
        }
    }
}
