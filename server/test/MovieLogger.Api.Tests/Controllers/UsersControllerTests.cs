using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MovieLogger.Api.Tests.TestSupport;
using MovieLogger.Service.Dtos.Users;

namespace MovieLogger.Api.Tests.Controllers
{
    [Collection(ApiTestCollection.Name)]
    public class UsersControllerTests(MovieLoggerApiFactory factory) : ApiTestBase(factory)
    {
        [Fact]
        public async Task Update_WithNewDetails_ReturnsNoContentAndPersists()
        {
            var user = await RegisterUserAsync("Ellen Ripley");
            var newEmail = UniqueEmail("ripley");

            var response = await user.Client.PutAsJsonAsync($"/api/users/{user.Id}", new UpdateUserDto
            {
                DisplayName = "Ripley",
                Email = newEmail
            });

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            var me = await ReadAsAsync<UserResponseDto>(await user.Client.GetAsync("/api/users/me"));
            me.DisplayName.Should().Be("Ripley");
            me.Email.Should().Be(newEmail);
        }

        [Fact]
        public async Task Update_KeepingOwnEmail_ReturnsNoContent()
        {
            var user = await RegisterUserAsync("Ellen Ripley");

            var response = await user.Client.PutAsJsonAsync($"/api/users/{user.Id}", new UpdateUserDto
            {
                DisplayName = "Ripley",
                Email = user.Email
            });

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Update_WithEmailOfAnotherUser_ReturnsConflictAndLeavesAccountUnchanged()
        {
            var alice = await RegisterUserAsync("Alice");
            var bob = await RegisterUserAsync("Bob");

            var response = await alice.Client.PutAsJsonAsync($"/api/users/{alice.Id}", new UpdateUserDto
            {
                DisplayName = "Alice",
                Email = bob.Email
            });

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            var me = await ReadAsAsync<UserResponseDto>(await alice.Client.GetAsync("/api/users/me"));
            me.Email.Should().Be(alice.Email);
        }

        [Fact]
        public async Task Update_AnotherUsersAccount_ReturnsForbidden()
        {
            var alice = await RegisterUserAsync("Alice");
            var bob = await RegisterUserAsync("Bob");

            var response = await alice.Client.PutAsJsonAsync($"/api/users/{bob.Id}", new UpdateUserDto
            {
                DisplayName = "Hijacked",
                Email = UniqueEmail()
            });

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}
