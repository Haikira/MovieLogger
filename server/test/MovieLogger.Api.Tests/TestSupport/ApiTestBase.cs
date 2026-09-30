using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using MovieLogger.Service.Dtos.Auth;
using MovieLogger.Service.Dtos.Movies;
using MovieLogger.Service.Dtos.MovieWatches;

namespace MovieLogger.Api.Tests.TestSupport
{
    /// <summary>
    /// Common HTTP helpers for the API tests. Everything goes through the real endpoints, so a user's
    /// token always comes from the API's own register/login flow rather than being minted by the tests.
    /// </summary>
    public abstract class ApiTestBase(MovieLoggerApiFactory factory)
    {
        protected const string DefaultPassword = "Correct-Horse-9";

        protected MovieLoggerApiFactory Factory { get; } = factory;

        protected HttpClient CreateClient() => Factory.CreateClient();

        protected HttpClient CreateAuthenticatedClient(string token)
        {
            var client = Factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        protected static string UniqueEmail(string prefix = "user") => $"{prefix}-{Guid.NewGuid():N}@movielogger.test";

        /// <summary>A value unique to this test, for titles etc. so searches only match this test's data.</summary>
        protected static string UniqueToken() => Guid.NewGuid().ToString("N")[..12];

        protected static RegisterRequestDto NewRegistration(string displayName = "Test User", string? email = null) => new()
        {
            DisplayName = displayName,
            Email = email ?? UniqueEmail(),
            Password = DefaultPassword,
            ConfirmPassword = DefaultPassword
        };

        protected async Task<TestUser> RegisterUserAsync(string displayName = "Test User")
        {
            var registration = NewRegistration(displayName);

            var response = await CreateClient().PostAsJsonAsync("/api/auth/register", registration);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var auth = (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
            return new TestUser(auth.User.Id, registration.Email, auth.Token, CreateAuthenticatedClient(auth.Token));
        }

        protected static async Task<MovieResponseDto> CreateMovieAsync(
            TestUser user, string title, int releaseYear = 2001, string? director = null, List<int>? genreIds = null)
        {
            var response = await user.Client.PostAsJsonAsync("/api/movies", new CreateMovieDto
            {
                Title = title,
                ReleaseYear = releaseYear,
                Director = director,
                RuntimeMinutes = 120,
                GenreIds = genreIds ?? []
            });
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            return (await response.Content.ReadFromJsonAsync<MovieResponseDto>())!;
        }

        protected static async Task<MovieWatchResponseDto> LogWatchAsync(
            TestUser user, int movieId, DateTime dateWatched, int? rating = null, string? notes = null)
        {
            var response = await user.Client.PostAsJsonAsync("/api/moviewatches", new CreateMovieWatchDto
            {
                MovieId = movieId,
                DateWatched = dateWatched,
                Rating = rating,
                Notes = notes
            });
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            return (await response.Content.ReadFromJsonAsync<MovieWatchResponseDto>())!;
        }

        protected static async Task<T> ReadAsAsync<T>(HttpResponseMessage response)
        {
            return (await response.Content.ReadFromJsonAsync<T>())!;
        }
    }
}
