using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MovieLogger.Api.Tests.TestSupport;
using MovieLogger.Service.Dtos.Common;
using MovieLogger.Service.Dtos.Movies;

namespace MovieLogger.Api.Tests.Controllers
{
    [Collection(ApiTestCollection.Name)]
    public class MoviesControllerTests(MovieLoggerApiFactory factory) : ApiTestBase(factory)
    {
        // Seeded by V2__seed_genres.sql.
        private const int DramaGenreId = 7;
        private const int ScienceFictionGenreId = 15;

        private static readonly DateTime WatchedOn = new(2025, 1, 10, 21, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task Create_WhenAuthenticated_ReturnsCreatedWithGenresAndCreator()
        {
            var user = await RegisterUserAsync();

            var response = await user.Client.PostAsJsonAsync("/api/movies", new CreateMovieDto
            {
                Title = "Gattaca",
                ReleaseYear = 1997,
                Director = "Andrew Niccol",
                GenreIds = [DramaGenreId, ScienceFictionGenreId]
            });

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var movie = await ReadAsAsync<MovieResponseDto>(response);
            movie.CreatedByUserId.Should().Be(user.Id);
            movie.Genres.Select(g => g.Name).Should().BeEquivalentTo("Drama", "Science Fiction");
        }

        [Fact]
        public async Task Create_WithoutToken_ReturnsUnauthorized()
        {
            var response = await CreateClient().PostAsJsonAsync("/api/movies", new CreateMovieDto
            {
                Title = "Unauthorised Movie",
                ReleaseYear = 2000
            });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Search_ByTitle_IsAnonymousAndReturnsMatchingMoviesOrderedByTitle()
        {
            var user = await RegisterUserAsync();
            var token = UniqueToken();
            await CreateMovieAsync(user, $"Charlie {token}");
            await CreateMovieAsync(user, $"Alpha {token}");
            await CreateMovieAsync(user, "Unrelated Title");

            var result = await SearchAsync($"?title={token}");

            result.TotalCount.Should().Be(2);
            result.Items.Select(m => m.Title).Should().Equal($"Alpha {token}", $"Charlie {token}");
        }

        [Fact]
        public async Task Search_WithPageSize_ReturnsRequestedPageAndTotalCount()
        {
            var user = await RegisterUserAsync();
            var token = UniqueToken();
            foreach (var prefix in new[] { "A", "B", "C" })
            {
                await CreateMovieAsync(user, $"{prefix} {token}");
            }

            var page1 = await SearchAsync($"?title={token}&page=1&pageSize=2");
            var page2 = await SearchAsync($"?title={token}&page=2&pageSize=2");

            page1.TotalCount.Should().Be(3);
            page1.Items.Select(m => m.Title).Should().Equal($"A {token}", $"B {token}");
            page2.TotalCount.Should().Be(3);
            page2.Items.Select(m => m.Title).Should().Equal($"C {token}");
        }

        [Fact]
        public async Task Search_ByDirectorAndYear_ReturnsOnlyMatchingMovies()
        {
            var user = await RegisterUserAsync();
            var director = $"Director {UniqueToken()}";
            var match = await CreateMovieAsync(user, "Match", releaseYear: 1999, director: director);
            await CreateMovieAsync(user, "Different Year", releaseYear: 2004, director: director);

            var result = await SearchAsync($"?director={Uri.EscapeDataString(director)}&year=1999");

            result.Items.Select(m => m.Id).Should().Equal(match.Id);
        }

        [Fact]
        public async Task Search_WithPageSizeAboveMaximum_ReturnsBadRequest()
        {
            var response = await CreateClient().GetAsync("/api/movies?pageSize=101");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetById_Anonymously_ReturnsMovieWithoutUserHistory()
        {
            var user = await RegisterUserAsync();
            var movie = await CreateMovieAsync(user, "Solaris", releaseYear: 1972);

            var response = await CreateClient().GetAsync($"/api/movies/{movie.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var details = await ReadAsAsync<MovieDetailsResponseDto>(response);
            details.Movie.Id.Should().Be(movie.Id);
            details.Movie.Title.Should().Be("Solaris");
            details.UserHistory.Should().BeNull();
        }

        [Fact]
        public async Task GetById_WhenAuthenticated_IncludesOnlyTheCallersOwnWatchHistory()
        {
            var alice = await RegisterUserAsync("Alice");
            var bob = await RegisterUserAsync("Bob");
            var movie = await CreateMovieAsync(alice, "Stalker");
            await LogWatchAsync(alice, movie.Id, WatchedOn, rating: 4);
            await LogWatchAsync(alice, movie.Id, WatchedOn.AddDays(7), rating: 5);

            var aliceDetails = await alice.Client.GetFromJsonAsync<MovieDetailsResponseDto>($"/api/movies/{movie.Id}");
            var bobDetails = await bob.Client.GetFromJsonAsync<MovieDetailsResponseDto>($"/api/movies/{movie.Id}");

            aliceDetails!.UserHistory!.TimesWatched.Should().Be(2);
            aliceDetails.UserHistory.LastRating.Should().Be(5);
            aliceDetails.UserHistory.Logs.Should().OnlyContain(l => l.UserId == alice.Id);
            bobDetails!.UserHistory!.TimesWatched.Should().Be(0);
            bobDetails.UserHistory.Logs.Should().BeEmpty();
        }

        [Fact]
        public async Task Update_WhenAuthenticated_ReplacesDetailsAndGenres()
        {
            var user = await RegisterUserAsync();
            var movie = await CreateMovieAsync(user, "Blade Runer", releaseYear: 1981, genreIds: [DramaGenreId]);

            var response = await user.Client.PutAsJsonAsync($"/api/movies/{movie.Id}", new UpdateMovieDto
            {
                Title = "Blade Runner",
                ReleaseYear = 1982,
                Director = "Ridley Scott",
                Synopsis = "A blade runner hunts four replicants in 2019 Los Angeles.",
                GenreIds = [ScienceFictionGenreId]
            });

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            var details = await CreateClient().GetFromJsonAsync<MovieDetailsResponseDto>($"/api/movies/{movie.Id}");
            details!.Movie.Title.Should().Be("Blade Runner");
            details.Movie.ReleaseYear.Should().Be(1982);
            details.Movie.Director.Should().Be("Ridley Scott");
            details.Movie.RuntimeMinutes.Should().BeNull();
            details.Movie.Genres.Select(g => g.Name).Should().Equal("Science Fiction");
            details.Movie.CreatedByUserId.Should().Be(user.Id);
        }

        [Fact]
        public async Task Update_WithInvalidGenreIds_ReturnsBadRequestAndLeavesMovieUnchanged()
        {
            var user = await RegisterUserAsync();
            var movie = await CreateMovieAsync(user, "Arrival", releaseYear: 2016, genreIds: [DramaGenreId]);

            var response = await user.Client.PutAsJsonAsync($"/api/movies/{movie.Id}", new UpdateMovieDto
            {
                Title = "Arrival (Edited)",
                ReleaseYear = 2016,
                GenreIds = [int.MaxValue]
            });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var details = await CreateClient().GetFromJsonAsync<MovieDetailsResponseDto>($"/api/movies/{movie.Id}");
            details!.Movie.Title.Should().Be("Arrival");
            details.Movie.Genres.Select(g => g.Name).Should().Equal("Drama");
        }

        [Fact]
        public async Task Update_WithoutTitle_ReturnsBadRequest()
        {
            var user = await RegisterUserAsync();
            var movie = await CreateMovieAsync(user, "Moon");

            var response = await user.Client.PutAsJsonAsync($"/api/movies/{movie.Id}", new UpdateMovieDto
            {
                Title = "",
                ReleaseYear = 2009
            });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Update_ForNonexistentMovie_ReturnsNotFound()
        {
            var user = await RegisterUserAsync();

            var response = await user.Client.PutAsJsonAsync($"/api/movies/{int.MaxValue}", new UpdateMovieDto
            {
                Title = "Nothing Here",
                ReleaseYear = 2000
            });

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Update_WithoutToken_ReturnsUnauthorized()
        {
            var user = await RegisterUserAsync();
            var movie = await CreateMovieAsync(user, "Sunshine");

            var response = await CreateClient().PutAsJsonAsync($"/api/movies/{movie.Id}", new UpdateMovieDto
            {
                Title = "Sunshine (Edited)",
                ReleaseYear = 2007
            });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Update_ByAnotherUser_ReturnsForbiddenAndLeavesMovieUnchanged()
        {
            var creator = await RegisterUserAsync("Creator");
            var other = await RegisterUserAsync("Other");
            var movie = await CreateMovieAsync(creator, "Heat", releaseYear: 1995);

            var response = await other.Client.PutAsJsonAsync($"/api/movies/{movie.Id}", new UpdateMovieDto
            {
                Title = "Vandalised",
                ReleaseYear = 1995
            });

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            var details = await CreateClient().GetFromJsonAsync<MovieDetailsResponseDto>($"/api/movies/{movie.Id}");
            details!.Movie.Title.Should().Be("Heat");
        }

        [Fact]
        public async Task Delete_ByCreator_ReturnsNoContentAndRemovesMovie()
        {
            var creator = await RegisterUserAsync();
            var movie = await CreateMovieAsync(creator, "Collateral");

            var response = await creator.Client.DeleteAsync($"/api/movies/{movie.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await CreateClient().GetAsync($"/api/movies/{movie.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Delete_ByAnotherUser_ReturnsForbiddenAndKeepsEveryonesHistory()
        {
            var creator = await RegisterUserAsync("Creator");
            var other = await RegisterUserAsync("Other");
            var movie = await CreateMovieAsync(creator, "Thief");
            await LogWatchAsync(creator, movie.Id, WatchedOn, rating: 4);

            var response = await other.Client.DeleteAsync($"/api/movies/{movie.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            var details = await creator.Client.GetFromJsonAsync<MovieDetailsResponseDto>($"/api/movies/{movie.Id}");
            details!.UserHistory!.TimesWatched.Should().Be(1);
        }

        [Fact]
        public async Task Delete_ForNonexistentMovie_ReturnsNotFound()
        {
            var user = await RegisterUserAsync();

            var response = await user.Client.DeleteAsync($"/api/movies/{int.MaxValue}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetById_ForNonexistentMovie_ReturnsNotFound()
        {
            var response = await CreateClient().GetAsync($"/api/movies/{int.MaxValue}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        private async Task<PagedResult<MovieResponseDto>> SearchAsync(string queryString)
        {
            var response = await CreateClient().GetAsync($"/api/movies{queryString}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            return await ReadAsAsync<PagedResult<MovieResponseDto>>(response);
        }
    }
}
