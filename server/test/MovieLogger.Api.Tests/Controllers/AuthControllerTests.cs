using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MovieLogger.Api.Tests.TestSupport;
using MovieLogger.Service.Dtos.Auth;
using MovieLogger.Service.Dtos.Users;
using MovieLogger.Service.Security;

namespace MovieLogger.Api.Tests.Controllers
{
    [Collection(ApiTestCollection.Name)]
    public class AuthControllerTests(MovieLoggerApiFactory factory) : ApiTestBase(factory)
    {
        [Fact]
        public async Task Register_WithValidDetails_ReturnsOkWithTokenAndUser()
        {
            var registration = NewRegistration("Ellen Ripley");

            var response = await CreateClient().PostAsJsonAsync("/api/auth/register", registration);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var auth = await ReadAsAsync<AuthResponseDto>(response);
            auth.Token.Should().NotBeNullOrWhiteSpace();
            auth.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
            auth.User.Id.Should().BePositive();
            auth.User.DisplayName.Should().Be("Ellen Ripley");
            auth.User.Email.Should().Be(registration.Email);
        }

        [Fact]
        public async Task Register_WithValidDetails_DoesNotReturnPasswordOrPasswordHash()
        {
            var registration = NewRegistration();

            var response = await CreateClient().PostAsJsonAsync("/api/auth/register", registration);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var json = await response.Content.ReadAsStringAsync();
            json.Should().NotContain(registration.Password);
            json.Should().NotContainEquivalentOf("passwordHash");

            var user = JsonDocument.Parse(json).RootElement.GetProperty("user");
            user.EnumerateObject().Select(p => p.Name)
                .Should().BeEquivalentTo("id", "displayName", "email", "createdAt");
        }

        [Fact]
        public async Task Register_WithEmailAlreadyInUse_ReturnsConflict()
        {
            var email = UniqueEmail();
            var client = CreateClient();
            (await client.PostAsJsonAsync("/api/auth/register", NewRegistration("First", email)))
                .StatusCode.Should().Be(HttpStatusCode.OK);

            var response = await client.PostAsJsonAsync("/api/auth/register", NewRegistration("Second", email));

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await response.Content.ReadAsStringAsync()).Should().Contain("already exists");
        }

        [Fact]
        public async Task Register_WithMismatchedPasswordConfirmation_ReturnsBadRequest()
        {
            var registration = NewRegistration();
            registration.ConfirmPassword = "Something-Else-1";

            var response = await CreateClient().PostAsJsonAsync("/api/auth/register", registration);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsJwtForThatUser()
        {
            var user = await RegisterUserAsync();

            var response = await CreateClient().PostAsJsonAsync("/api/auth/login",
                new LoginRequestDto { Email = user.Email, Password = DefaultPassword });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var auth = await ReadAsAsync<AuthResponseDto>(response);
            auth.User.Id.Should().Be(user.Id);

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(auth.Token);
            var settings = JwtSettings;
            jwt.Subject.Should().Be(user.Id.ToString());
            jwt.Issuer.Should().Be(settings.Issuer);
            jwt.Audiences.Should().ContainSingle().Which.Should().Be(settings.Audience);
            jwt.ValidTo.Should().BeAfter(DateTime.UtcNow);
        }

        [Fact]
        public async Task Login_WithIncorrectPassword_ReturnsUnauthorized()
        {
            var user = await RegisterUserAsync();

            var response = await CreateClient().PostAsJsonAsync("/api/auth/login",
                new LoginRequestDto { Email = user.Email, Password = "Wrong-Password-1" });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await response.Content.ReadAsStringAsync()).Should().NotContain("token");
        }

        [Fact]
        public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
        {
            var response = await CreateClient().PostAsJsonAsync("/api/auth/login",
                new LoginRequestDto { Email = UniqueEmail("nobody"), Password = DefaultPassword });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
        {
            var response = await CreateClient().GetAsync("/api/users/me");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ProtectedEndpoint_WithTokenSignedByAnotherKey_ReturnsUnauthorized()
        {
            var user = await RegisterUserAsync();
            var forgedToken = CreateTokenSignedWithUnknownKey(user.Id);

            var response = await CreateAuthenticatedClient(forgedToken).GetAsync("/api/users/me");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ProtectedEndpoint_WithTokenFromLogin_ReturnsAuthenticatedUser()
        {
            var user = await RegisterUserAsync("Dallas");
            var loginResponse = await CreateClient().PostAsJsonAsync("/api/auth/login",
                new LoginRequestDto { Email = user.Email, Password = DefaultPassword });
            var token = (await ReadAsAsync<AuthResponseDto>(loginResponse)).Token;

            var response = await CreateAuthenticatedClient(token).GetAsync("/api/users/me");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var me = await ReadAsAsync<UserResponseDto>(response);
            me.Id.Should().Be(user.Id);
            me.Email.Should().Be(user.Email);
            me.DisplayName.Should().Be("Dallas");
        }

        [Fact]
        public async Task GetUserById_ForAnotherUser_ReturnsForbidden()
        {
            var alice = await RegisterUserAsync("Alice");
            var bob = await RegisterUserAsync("Bob");

            var response = await bob.Client.GetAsync($"/api/users/{alice.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        private JwtSettings JwtSettings => Factory.Services.GetRequiredService<IOptions<JwtSettings>>().Value;

        // Same claims, issuer and audience the API issues, but signed with a key the API doesn't trust.
        private string CreateTokenSignedWithUnknownKey(int userId)
        {
            var settings = JwtSettings;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(new string('x', 64)));
            var token = new JwtSecurityToken(
                issuer: settings.Issuer,
                audience: settings.Audience,
                claims: [new(JwtRegisteredClaimNames.Sub, userId.ToString())],
                expires: DateTime.UtcNow.AddMinutes(10),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
