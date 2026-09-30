using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MovieLogger.Api.Tests.TestSupport;
using MovieLogger.Service.Dtos.Lists;

namespace MovieLogger.Api.Tests.Controllers
{
    [Collection(ApiTestCollection.Name)]
    public class ListsControllerTests(MovieLoggerApiFactory factory) : ApiTestBase(factory)
    {
        [Fact]
        public async Task Create_WhenAuthenticated_ReturnsCreatedListOwnedByTheAuthenticatedUser()
        {
            var user = await RegisterUserAsync();

            var response = await user.Client.PostAsJsonAsync("/api/lists",
                new CreateMovieListDto { Name = "Comfort Films", Description = "For rainy days" });

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await ReadAsAsync<MovieListResponseDto>(response);
            created.UserId.Should().Be(user.Id);
            response.Headers.Location!.AbsolutePath.Should().EndWithEquivalentOf($"/api/Lists/{created.Id}");

            var stored = await user.Client.GetFromJsonAsync<MovieListResponseDto>($"/api/lists/{created.Id}");
            stored!.UserId.Should().Be(user.Id);
            stored.Name.Should().Be("Comfort Films");
            stored.Description.Should().Be("For rainy days");
        }

        [Fact]
        public async Task Create_WithUserIdInRequestBody_IgnoresItAndUsesTheJwtIdentity()
        {
            var alice = await RegisterUserAsync("Alice");
            var bob = await RegisterUserAsync("Bob");

            // A malicious client tries to create a list in Bob's account.
            var response = await alice.Client.PostAsJsonAsync("/api/lists", new { userId = bob.Id, name = "Planted List" });

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            (await ReadAsAsync<MovieListResponseDto>(response)).UserId.Should().Be(alice.Id);
            (await GetListsAsync(bob)).Should().BeEmpty();
        }

        [Fact]
        public async Task GetAll_ReturnsOnlyTheAuthenticatedUsersLists()
        {
            var alice = await RegisterUserAsync("Alice");
            var bob = await RegisterUserAsync("Bob");
            var aliceList = await CreateListAsync(alice, "Alice's List");
            var bobList = await CreateListAsync(bob, "Bob's List");

            var aliceLists = await GetListsAsync(alice);
            var bobLists = await GetListsAsync(bob);

            aliceLists.Select(l => l.Id).Should().Equal(aliceList.Id);
            bobLists.Select(l => l.Id).Should().Equal(bobList.Id);
        }

        [Fact]
        public async Task GetById_ForAnotherUsersList_ReturnsNotFound()
        {
            var alice = await RegisterUserAsync("Alice");
            var bob = await RegisterUserAsync("Bob");
            var aliceList = await CreateListAsync(alice, "Private");

            var response = await bob.Client.GetAsync($"/api/lists/{aliceList.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Update_OwnList_ReturnsNoContentAndPersistsChanges()
        {
            var user = await RegisterUserAsync();
            var list = await CreateListAsync(user, "Old Name");

            var response = await user.Client.PutAsJsonAsync($"/api/lists/{list.Id}",
                new UpdateMovieListDto { Name = "New Name", Description = "Updated" });

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            var stored = await user.Client.GetFromJsonAsync<MovieListResponseDto>($"/api/lists/{list.Id}");
            stored!.Name.Should().Be("New Name");
            stored.Description.Should().Be("Updated");
        }

        [Fact]
        public async Task Update_AnotherUsersList_ReturnsNotFoundAndLeavesItUnchanged()
        {
            var alice = await RegisterUserAsync("Alice");
            var bob = await RegisterUserAsync("Bob");
            var aliceList = await CreateListAsync(alice, "Alice's Name");

            var response = await bob.Client.PutAsJsonAsync($"/api/lists/{aliceList.Id}",
                new UpdateMovieListDto { Name = "Renamed by Bob" });

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var stored = await alice.Client.GetFromJsonAsync<MovieListResponseDto>($"/api/lists/{aliceList.Id}");
            stored!.Name.Should().Be("Alice's Name");
        }

        [Fact]
        public async Task Delete_OwnList_ReturnsNoContentAndRemovesIt()
        {
            var user = await RegisterUserAsync();
            var movie = await CreateMovieAsync(user, "Paddington 2");
            var list = await CreateListAsync(user, "To Delete");
            await user.Client.PostAsync($"/api/lists/{list.Id}/movies/{movie.Id}", null);

            var response = await user.Client.DeleteAsync($"/api/lists/{list.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await user.Client.GetAsync($"/api/lists/{list.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Delete_AnotherUsersList_ReturnsNotFoundAndLeavesItInPlace()
        {
            var alice = await RegisterUserAsync("Alice");
            var bob = await RegisterUserAsync("Bob");
            var aliceList = await CreateListAsync(alice, "Keep Me");

            var response = await bob.Client.DeleteAsync($"/api/lists/{aliceList.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await alice.Client.GetAsync($"/api/lists/{aliceList.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task AddAndRemoveMovie_OnOwnList_UpdatesTheListsMovies()
        {
            var user = await RegisterUserAsync();
            var movie = await CreateMovieAsync(user, "Amélie");
            var list = await CreateListAsync(user, "French Films");

            var addResponse = await user.Client.PostAsync($"/api/lists/{list.Id}/movies/{movie.Id}", null);
            var afterAdd = await user.Client.GetFromJsonAsync<MovieListResponseDto>($"/api/lists/{list.Id}");
            var removeResponse = await user.Client.DeleteAsync($"/api/lists/{list.Id}/movies/{movie.Id}");
            var afterRemove = await user.Client.GetFromJsonAsync<MovieListResponseDto>($"/api/lists/{list.Id}");

            addResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
            afterAdd!.Movies.Select(m => m.Id).Should().Equal(movie.Id);
            removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
            afterRemove!.Movies.Should().BeEmpty();
        }

        [Fact]
        public async Task AddMovie_ToAnotherUsersList_ReturnsNotFoundAndLeavesListUnchanged()
        {
            var alice = await RegisterUserAsync("Alice");
            var bob = await RegisterUserAsync("Bob");
            var movie = await CreateMovieAsync(bob, "Bob's Movie");
            var aliceList = await CreateListAsync(alice, "Alice's Picks");

            var response = await bob.Client.PostAsync($"/api/lists/{aliceList.Id}/movies/{movie.Id}", null);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var stored = await alice.Client.GetFromJsonAsync<MovieListResponseDto>($"/api/lists/{aliceList.Id}");
            stored!.Movies.Should().BeEmpty();
        }

        [Fact]
        public async Task RemoveMovie_FromAnotherUsersList_ReturnsNotFoundAndLeavesMovieOnList()
        {
            var alice = await RegisterUserAsync("Alice");
            var bob = await RegisterUserAsync("Bob");
            var movie = await CreateMovieAsync(alice, "Alice's Movie");
            var aliceList = await CreateListAsync(alice, "Alice's Picks");
            await alice.Client.PostAsync($"/api/lists/{aliceList.Id}/movies/{movie.Id}", null);

            var response = await bob.Client.DeleteAsync($"/api/lists/{aliceList.Id}/movies/{movie.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var stored = await alice.Client.GetFromJsonAsync<MovieListResponseDto>($"/api/lists/{aliceList.Id}");
            stored!.Movies.Select(m => m.Id).Should().Equal(movie.Id);
        }

        [Fact]
        public async Task ListEndpoints_WithoutToken_ReturnUnauthorizedAndChangeNothing()
        {
            var user = await RegisterUserAsync();
            var movie = await CreateMovieAsync(user, "Anonymous Target");
            var list = await CreateListAsync(user, "Original Name");
            var anonymous = CreateClient();

            var responses = new[]
            {
                await anonymous.GetAsync("/api/lists"),
                await anonymous.GetAsync($"/api/lists/{list.Id}"),
                await anonymous.PostAsJsonAsync("/api/lists", new { userId = user.Id, name = "Anonymous List" }),
                await anonymous.PutAsJsonAsync($"/api/lists/{list.Id}", new UpdateMovieListDto { Name = "Renamed" }),
                await anonymous.PostAsync($"/api/lists/{list.Id}/movies/{movie.Id}", null),
                await anonymous.DeleteAsync($"/api/lists/{list.Id}/movies/{movie.Id}"),
                await anonymous.DeleteAsync($"/api/lists/{list.Id}")
            };

            responses.Select(r => r.StatusCode).Should().AllBeEquivalentTo(HttpStatusCode.Unauthorized);
            var lists = await GetListsAsync(user);
            var stored = lists.Should().ContainSingle().Subject;
            stored.Name.Should().Be("Original Name");
            stored.Movies.Should().BeEmpty();
        }

        private static async Task<MovieListResponseDto> CreateListAsync(TestUser user, string name)
        {
            var response = await user.Client.PostAsJsonAsync("/api/lists", new CreateMovieListDto { Name = name });
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            return (await response.Content.ReadFromJsonAsync<MovieListResponseDto>())!;
        }

        private static async Task<List<MovieListResponseDto>> GetListsAsync(TestUser user)
        {
            return (await user.Client.GetFromJsonAsync<List<MovieListResponseDto>>("/api/lists"))!;
        }
    }
}
