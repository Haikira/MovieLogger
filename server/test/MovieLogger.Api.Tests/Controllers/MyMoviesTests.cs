using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MovieLogger.Api.Tests.TestSupport;
using MovieLogger.Service.Dtos.Common;
using MovieLogger.Service.Dtos.MovieWatches;

namespace MovieLogger.Api.Tests.Controllers
{
    /// <summary>GET /api/moviewatches/my-movies: the user's watched movies, one row per movie.</summary>
    [Collection(ApiTestCollection.Name)]
    public class MyMoviesTests(MovieLoggerApiFactory factory) : ApiTestBase(factory)
    {
        private static readonly DateTime WatchedOn = new(2025, 3, 1, 19, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task GetMyMovies_ReturnsOneRowPerMovieWithLatestRatingAndWatchCount()
        {
            var user = await RegisterUserAsync();
            var movie = await CreateMovieAsync(user, "Blade Runner", releaseYear: 1982, director: "Ridley Scott");
            await LogWatchAsync(user, movie.Id, WatchedOn, rating: 3);
            await LogWatchAsync(user, movie.Id, WatchedOn.AddMonths(2), rating: 5);

            var result = await GetMyMoviesAsync(user, "");

            result.TotalCount.Should().Be(1);
            var row = result.Items.Should().ContainSingle().Subject;
            row.MovieId.Should().Be(movie.Id);
            row.Title.Should().Be("Blade Runner");
            row.ReleaseYear.Should().Be(1982);
            row.Director.Should().Be("Ridley Scott");
            row.TimesWatched.Should().Be(2);
            row.LastRating.Should().Be(5);
            row.LastWatchedAt.Should().Be(WatchedOn.AddMonths(2));
        }

        [Fact]
        public async Task GetMyMovies_OnlyIncludesMoviesTheAuthenticatedUserLogged()
        {
            var alice = await RegisterUserAsync("Alice");
            var bob = await RegisterUserAsync("Bob");
            var aliceMovie = await CreateMovieAsync(alice, "Alice's Pick");
            var bobMovie = await CreateMovieAsync(bob, "Bob's Pick");
            await LogWatchAsync(alice, aliceMovie.Id, WatchedOn, rating: 4);
            await LogWatchAsync(bob, bobMovie.Id, WatchedOn, rating: 2);

            var aliceResult = await GetMyMoviesAsync(alice, "");
            var bobResult = await GetMyMoviesAsync(bob, "");

            aliceResult.Items.Select(i => i.MovieId).Should().Equal(aliceMovie.Id);
            bobResult.Items.Select(i => i.MovieId).Should().Equal(bobMovie.Id);
        }

        [Fact]
        public async Task GetMyMovies_WithPageSize_ReturnsRequestedPageAndTotalCount()
        {
            var user = await RegisterUserAsync();
            var movieIds = new List<int>();
            for (var i = 0; i < 3; i++)
            {
                var movie = await CreateMovieAsync(user, $"Paged Movie {i}");
                await LogWatchAsync(user, movie.Id, WatchedOn.AddDays(i));
                movieIds.Add(movie.Id);
            }

            // Default sort is most recently watched first.
            var page1 = await GetMyMoviesAsync(user, "?page=1&pageSize=2");
            var page2 = await GetMyMoviesAsync(user, "?page=2&pageSize=2");

            page1.TotalCount.Should().Be(3);
            page1.Page.Should().Be(1);
            page1.PageSize.Should().Be(2);
            page1.Items.Select(i => i.MovieId).Should().Equal(movieIds[2], movieIds[1]);
            page2.TotalCount.Should().Be(3);
            page2.Items.Select(i => i.MovieId).Should().Equal(movieIds[0]);
        }

        [Fact]
        public async Task GetMyMovies_WithSearchAndRatingFilters_ReturnsOnlyMatchingMovies()
        {
            var user = await RegisterUserAsync();
            var token = UniqueToken();
            var match = await CreateMovieAsync(user, $"Dune {token}");
            var wrongRating = await CreateMovieAsync(user, $"Dune Part Two {token}");
            var wrongTitle = await CreateMovieAsync(user, "Sicario");
            await LogWatchAsync(user, match.Id, WatchedOn, rating: 4);
            await LogWatchAsync(user, wrongRating.Id, WatchedOn, rating: 2);
            await LogWatchAsync(user, wrongTitle.Id, WatchedOn, rating: 4);

            var bySearch = await GetMyMoviesAsync(user, $"?search={token}");
            var bySearchAndRating = await GetMyMoviesAsync(user, $"?search={token}&rating=4");

            bySearch.Items.Select(i => i.MovieId).Should().BeEquivalentTo([match.Id, wrongRating.Id]);
            bySearchAndRating.Items.Select(i => i.MovieId).Should().Equal(match.Id);
        }

        [Fact]
        public async Task GetMyMovies_WithInvalidPageSize_ReturnsBadRequest()
        {
            var user = await RegisterUserAsync();

            var response = await user.Client.GetAsync("/api/moviewatches/my-movies?pageSize=0");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetMyMovies_WithoutToken_ReturnsUnauthorized()
        {
            var response = await CreateClient().GetAsync("/api/moviewatches/my-movies");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        private static async Task<PagedResult<MyMovieResponseDto>> GetMyMoviesAsync(TestUser user, string queryString)
        {
            var response = await user.Client.GetAsync($"/api/moviewatches/my-movies{queryString}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            return (await response.Content.ReadFromJsonAsync<PagedResult<MyMovieResponseDto>>())!;
        }
    }
}
