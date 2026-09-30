using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MovieLogger.Api.Tests.TestSupport;
using MovieLogger.Service.Dtos.Dashboard;

namespace MovieLogger.Api.Tests.Controllers
{
    [Collection(ApiTestCollection.Name)]
    public class DashboardControllerTests(MovieLoggerApiFactory factory) : ApiTestBase(factory)
    {
        // Seeded by V2__seed_genres.sql.
        private const int HorrorGenreId = 10;

        private static readonly DateTime WatchedOn = new(2025, 10, 31, 22, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task Get_ReturnsStatsForTheAuthenticatedUserOnly()
        {
            var alice = await RegisterUserAsync("Alice");
            var bob = await RegisterUserAsync("Bob");
            var movie = await CreateMovieAsync(alice, "The Shining", genreIds: [HorrorGenreId]);
            var watchlistMovie = await CreateMovieAsync(alice, "Suspiria");
            await LogWatchAsync(alice, movie.Id, WatchedOn.AddYears(-1), rating: 2);
            var latest = await LogWatchAsync(alice, movie.Id, WatchedOn, rating: 4);
            await alice.Client.PostAsync($"/api/watchlist/{watchlistMovie.Id}", null);

            var aliceDashboard = await alice.Client.GetFromJsonAsync<DashboardResponseDto>("/api/dashboard");
            var bobDashboard = await bob.Client.GetFromJsonAsync<DashboardResponseDto>("/api/dashboard");

            aliceDashboard!.TotalMoviesLogged.Should().Be(2);
            aliceDashboard.AverageRating.Should().Be(3);
            aliceDashboard.WatchlistCount.Should().Be(1);
            aliceDashboard.RecentlyWatched.Select(w => w.Id).Should().StartWith(latest.Id);
            aliceDashboard.TopGenres.Should().ContainSingle()
                .Which.Should().BeEquivalentTo(new GenreCountDto { GenreId = HorrorGenreId, GenreName = "Horror", Count = 2 });

            bobDashboard!.TotalMoviesLogged.Should().Be(0);
            bobDashboard.AverageRating.Should().BeNull();
            bobDashboard.WatchlistCount.Should().Be(0);
            bobDashboard.RecentlyWatched.Should().BeEmpty();
            bobDashboard.TopGenres.Should().BeEmpty();
        }

        [Fact]
        public async Task Get_WithoutToken_ReturnsUnauthorized()
        {
            var response = await CreateClient().GetAsync("/api/dashboard");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
