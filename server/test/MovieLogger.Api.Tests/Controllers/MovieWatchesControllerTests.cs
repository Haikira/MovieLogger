using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MovieLogger.Api.Tests.TestSupport;
using MovieLogger.Service.Dtos.MovieWatches;

namespace MovieLogger.Api.Tests.Controllers
{
    [Collection(ApiTestCollection.Name)]
    public class MovieWatchesControllerTests(MovieLoggerApiFactory factory) : ApiTestBase(factory)
    {
        private static readonly DateTime WatchedOn = new(2025, 6, 14, 20, 30, 0, DateTimeKind.Utc);

        [Fact]
        public async Task Create_WithValidLog_ReturnsCreatedAndPersistsItForTheAuthenticatedUser()
        {
            var user = await RegisterUserAsync();
            var movie = await CreateMovieAsync(user, "Alien");

            var response = await user.Client.PostAsJsonAsync("/api/moviewatches", new CreateMovieWatchDto
            {
                MovieId = movie.Id,
                DateWatched = WatchedOn,
                Rating = 5,
                Notes = "Still terrifying."
            });

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await ReadAsAsync<MovieWatchResponseDto>(response);
            created.UserId.Should().Be(user.Id);
            response.Headers.Location!.AbsolutePath.Should().EndWithEquivalentOf($"/api/MovieWatches/{created.Id}");

            // Read it back through the API to prove what was stored, not just what was echoed back.
            var stored = await user.Client.GetFromJsonAsync<MovieWatchResponseDto>($"/api/moviewatches/{created.Id}");
            stored!.UserId.Should().Be(user.Id);
            stored.MovieId.Should().Be(movie.Id);
            stored.DateWatched.Should().Be(WatchedOn);
            stored.Rating.Should().Be(5);
            stored.Notes.Should().Be("Still terrifying.");
        }

        [Fact]
        public async Task Create_WithUserIdInRequestBody_IgnoresItAndUsesTheJwtIdentity()
        {
            var alice = await RegisterUserAsync("Alice");
            var bob = await RegisterUserAsync("Bob");
            var movie = await CreateMovieAsync(alice, "Aliens");

            // A malicious client tries to log a watch against Bob's account.
            var response = await alice.Client.PostAsJsonAsync("/api/moviewatches", new
            {
                userId = bob.Id,
                movieId = movie.Id,
                dateWatched = WatchedOn,
                rating = 4
            });

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            (await ReadAsAsync<MovieWatchResponseDto>(response)).UserId.Should().Be(alice.Id);
            (await bob.Client.GetFromJsonAsync<List<MovieWatchResponseDto>>("/api/moviewatches")).Should().BeEmpty();
        }

        [Fact]
        public async Task Create_SameMovieMoreThanOnce_KeepsEveryLog()
        {
            var user = await RegisterUserAsync();
            var movie = await CreateMovieAsync(user, "The Thing");

            var first = await LogWatchAsync(user, movie.Id, WatchedOn.AddYears(-1), rating: 4, notes: "First time");
            var second = await LogWatchAsync(user, movie.Id, WatchedOn, rating: 5, notes: "Even better");

            var logs = await user.Client.GetFromJsonAsync<List<MovieWatchResponseDto>>("/api/moviewatches");
            logs!.Select(l => l.Id).Should().BeEquivalentTo([second.Id, first.Id], o => o.WithStrictOrdering());
            logs.Should().OnlyContain(l => l.MovieId == movie.Id && l.UserId == user.Id);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        public async Task Create_WithRatingOutsideOneToFive_ReturnsBadRequest(int rating)
        {
            var user = await RegisterUserAsync();
            var movie = await CreateMovieAsync(user, "Halloween");

            var response = await user.Client.PostAsJsonAsync("/api/moviewatches", new CreateMovieWatchDto
            {
                MovieId = movie.Id,
                DateWatched = WatchedOn,
                Rating = rating
            });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await response.Content.ReadAsStringAsync()).Should().Contain("Rating");
            (await user.Client.GetFromJsonAsync<List<MovieWatchResponseDto>>("/api/moviewatches")).Should().BeEmpty();
        }

        [Fact]
        public async Task Create_WithDateWatchedInTheFuture_ReturnsBadRequest()
        {
            var user = await RegisterUserAsync();
            var movie = await CreateMovieAsync(user, "Back to the Future");

            var response = await user.Client.PostAsJsonAsync("/api/moviewatches", new CreateMovieWatchDto
            {
                MovieId = movie.Id,
                DateWatched = DateTime.UtcNow.AddDays(1),
                Rating = 3
            });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await response.Content.ReadAsStringAsync()).Should().Contain("cannot be in the future");
        }

        [Fact]
        public async Task Create_ForNonexistentMovie_ReturnsBadRequest()
        {
            var user = await RegisterUserAsync();

            var response = await user.Client.PostAsJsonAsync("/api/moviewatches", new CreateMovieWatchDto
            {
                MovieId = int.MaxValue,
                DateWatched = WatchedOn
            });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Create_WithoutToken_ReturnsUnauthorized()
        {
            var user = await RegisterUserAsync();
            var movie = await CreateMovieAsync(user, "Jaws");

            var response = await CreateClient().PostAsJsonAsync("/api/moviewatches", new CreateMovieWatchDto
            {
                MovieId = movie.Id,
                DateWatched = WatchedOn
            });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetAll_ReturnsOnlyTheAuthenticatedUsersLogs()
        {
            var alice = await RegisterUserAsync("Alice");
            var bob = await RegisterUserAsync("Bob");
            var movie = await CreateMovieAsync(alice, "Heat");
            var aliceLog = await LogWatchAsync(alice, movie.Id, WatchedOn, rating: 5);
            var bobLog = await LogWatchAsync(bob, movie.Id, WatchedOn, rating: 2);

            var aliceLogs = await alice.Client.GetFromJsonAsync<List<MovieWatchResponseDto>>("/api/moviewatches");
            var bobLogs = await bob.Client.GetFromJsonAsync<List<MovieWatchResponseDto>>("/api/moviewatches");

            aliceLogs!.Select(l => l.Id).Should().Equal(aliceLog.Id);
            bobLogs!.Select(l => l.Id).Should().Equal(bobLog.Id);
        }

        [Fact]
        public async Task GetById_ForAnotherUsersLog_ReturnsNotFound()
        {
            var alice = await RegisterUserAsync("Alice");
            var bob = await RegisterUserAsync("Bob");
            var movie = await CreateMovieAsync(alice, "Se7en");
            var aliceLog = await LogWatchAsync(alice, movie.Id, WatchedOn, rating: 5);

            var response = await bob.Client.GetAsync($"/api/moviewatches/{aliceLog.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Update_OwnLog_ReturnsNoContentAndPersistsChanges()
        {
            var user = await RegisterUserAsync();
            var movie = await CreateMovieAsync(user, "Zodiac");
            var log = await LogWatchAsync(user, movie.Id, WatchedOn, rating: 3, notes: "Long");

            var response = await user.Client.PutAsJsonAsync($"/api/moviewatches/{log.Id}", new UpdateMovieWatchDto
            {
                DateWatched = WatchedOn,
                Rating = 4,
                Notes = "Better second time"
            });

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            var stored = await user.Client.GetFromJsonAsync<MovieWatchResponseDto>($"/api/moviewatches/{log.Id}");
            stored!.Rating.Should().Be(4);
            stored.Notes.Should().Be("Better second time");
            stored.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task Update_AnotherUsersLog_ReturnsNotFoundAndLeavesItUnchanged()
        {
            var alice = await RegisterUserAsync("Alice");
            var bob = await RegisterUserAsync("Bob");
            var movie = await CreateMovieAsync(alice, "Fargo");
            var aliceLog = await LogWatchAsync(alice, movie.Id, WatchedOn, rating: 5, notes: "Alice's notes");

            var response = await bob.Client.PutAsJsonAsync($"/api/moviewatches/{aliceLog.Id}", new UpdateMovieWatchDto
            {
                DateWatched = WatchedOn,
                Rating = 1,
                Notes = "Overwritten by Bob"
            });

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var stored = await alice.Client.GetFromJsonAsync<MovieWatchResponseDto>($"/api/moviewatches/{aliceLog.Id}");
            stored!.Rating.Should().Be(5);
            stored.Notes.Should().Be("Alice's notes");
            stored.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public async Task Delete_OwnLog_ReturnsNoContentAndRemovesIt()
        {
            var user = await RegisterUserAsync();
            var movie = await CreateMovieAsync(user, "Memento");
            var log = await LogWatchAsync(user, movie.Id, WatchedOn);

            var response = await user.Client.DeleteAsync($"/api/moviewatches/{log.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await user.Client.GetAsync($"/api/moviewatches/{log.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Delete_AnotherUsersLog_ReturnsNotFoundAndLeavesItInPlace()
        {
            var alice = await RegisterUserAsync("Alice");
            var bob = await RegisterUserAsync("Bob");
            var movie = await CreateMovieAsync(alice, "Arrival");
            var aliceLog = await LogWatchAsync(alice, movie.Id, WatchedOn, rating: 5);

            var response = await bob.Client.DeleteAsync($"/api/moviewatches/{aliceLog.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await alice.Client.GetAsync($"/api/moviewatches/{aliceLog.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
