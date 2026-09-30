using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MovieLogger.Api.Tests.TestSupport;
using MovieLogger.Service.Dtos.Watchlist;

namespace MovieLogger.Api.Tests.Controllers
{
    [Collection(ApiTestCollection.Name)]
    public class WatchlistControllerTests(MovieLoggerApiFactory factory) : ApiTestBase(factory)
    {
        [Fact]
        public async Task Add_MovieNotYetOnWatchlist_ReturnsCreatedAndAppearsInWatchlist()
        {
            var user = await RegisterUserAsync();
            var movie = await CreateMovieAsync(user, "Oppenheimer", releaseYear: 2023);

            var response = await user.Client.PostAsync($"/api/watchlist/{movie.Id}", null);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var added = await ReadAsAsync<WatchlistItemResponseDto>(response);
            added.MovieId.Should().Be(movie.Id);
            added.Title.Should().Be("Oppenheimer");
            added.ReleaseYear.Should().Be(2023);

            var watchlist = await GetWatchlistAsync(user);
            watchlist.Should().ContainSingle().Which.MovieId.Should().Be(movie.Id);
        }

        [Fact]
        public async Task Add_MovieAlreadyOnWatchlist_ReturnsConflictWithoutDuplicating()
        {
            var user = await RegisterUserAsync();
            var movie = await CreateMovieAsync(user, "Tenet");
            (await user.Client.PostAsync($"/api/watchlist/{movie.Id}", null)).StatusCode.Should().Be(HttpStatusCode.Created);

            var response = await user.Client.PostAsync($"/api/watchlist/{movie.Id}", null);

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await GetWatchlistAsync(user)).Should().ContainSingle();
        }

        [Fact]
        public async Task Add_NonexistentMovie_ReturnsBadRequest()
        {
            var user = await RegisterUserAsync();

            var response = await user.Client.PostAsync($"/api/watchlist/{int.MaxValue}", null);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Remove_MovieOnWatchlist_ReturnsNoContentAndRemovesIt()
        {
            var user = await RegisterUserAsync();
            var movie = await CreateMovieAsync(user, "Inception");
            await user.Client.PostAsync($"/api/watchlist/{movie.Id}", null);

            var response = await user.Client.DeleteAsync($"/api/watchlist/{movie.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await GetWatchlistAsync(user)).Should().BeEmpty();
        }

        [Fact]
        public async Task Remove_MovieNotOnWatchlist_ReturnsNotFound()
        {
            var user = await RegisterUserAsync();
            var movie = await CreateMovieAsync(user, "Interstellar");

            var response = await user.Client.DeleteAsync($"/api/watchlist/{movie.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Watchlists_AreIsolatedBetweenUsers()
        {
            var alice = await RegisterUserAsync("Alice");
            var bob = await RegisterUserAsync("Bob");
            var movie = await CreateMovieAsync(alice, "Dunkirk");
            await alice.Client.PostAsync($"/api/watchlist/{movie.Id}", null);

            var bobWatchlist = await GetWatchlistAsync(bob);
            var bobRemoveResponse = await bob.Client.DeleteAsync($"/api/watchlist/{movie.Id}");

            bobWatchlist.Should().BeEmpty();
            bobRemoveResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await GetWatchlistAsync(alice)).Should().ContainSingle().Which.MovieId.Should().Be(movie.Id);
        }

        [Fact]
        public async Task SameMovie_CanBeOnTwoUsersWatchlists()
        {
            var alice = await RegisterUserAsync("Alice");
            var bob = await RegisterUserAsync("Bob");
            var movie = await CreateMovieAsync(alice, "Insomnia");
            await alice.Client.PostAsync($"/api/watchlist/{movie.Id}", null);

            var response = await bob.Client.PostAsync($"/api/watchlist/{movie.Id}", null);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task WatchlistEndpoints_WithoutToken_ReturnUnauthorized()
        {
            var user = await RegisterUserAsync();
            var movie = await CreateMovieAsync(user, "Following");
            var client = CreateClient();

            (await client.GetAsync("/api/watchlist")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await client.PostAsync($"/api/watchlist/{movie.Id}", null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await client.DeleteAsync($"/api/watchlist/{movie.Id}")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        private static async Task<List<WatchlistItemResponseDto>> GetWatchlistAsync(TestUser user)
        {
            return (await user.Client.GetFromJsonAsync<List<WatchlistItemResponseDto>>("/api/watchlist"))!;
        }
    }
}
