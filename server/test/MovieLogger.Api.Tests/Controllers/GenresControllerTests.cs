using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MovieLogger.Api.Tests.TestSupport;
using MovieLogger.Service.Dtos.Genres;
using MovieLogger.Service.Dtos.Movies;

namespace MovieLogger.Api.Tests.Controllers
{
    [Collection(ApiTestCollection.Name)]
    public class GenresControllerTests(MovieLoggerApiFactory factory) : ApiTestBase(factory)
    {
        // Seeded by V2__seed_genres.sql.
        private const int DramaGenreId = 7;
        private const int HorrorGenreId = 10;

        [Fact]
        public async Task GetAll_IsAnonymousAndIncludesSeededGenres()
        {
            var response = await CreateClient().GetAsync("/api/genres");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var genres = await ReadAsAsync<List<GenreResponseDto>>(response);
            genres.Should().ContainEquivalentOf(new GenreResponseDto { Id = DramaGenreId, Name = "Drama" });
            genres.Should().ContainEquivalentOf(new GenreResponseDto { Id = HorrorGenreId, Name = "Horror" });
        }

        [Fact]
        public async Task GetById_SeededGenre_IsAnonymousAndReturnsGenre()
        {
            var response = await CreateClient().GetAsync($"/api/genres/{DramaGenreId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var genre = await ReadAsAsync<GenreResponseDto>(response);
            genre.Should().BeEquivalentTo(new GenreResponseDto { Id = DramaGenreId, Name = "Drama" });
        }

        [Fact]
        public async Task GetById_NonexistentGenre_ReturnsNotFound()
        {
            var response = await CreateClient().GetAsync($"/api/genres/{int.MaxValue}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Create_ValidName_ReturnsCreatedWithLocationAndAppearsInGenres()
        {
            var user = await RegisterUserAsync();
            var name = $"Genre {UniqueToken()}";

            var response = await user.Client.PostAsJsonAsync("/api/genres", new CreateGenreDto { Name = name });

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await ReadAsAsync<GenreResponseDto>(response);
            created.Id.Should().BePositive();
            created.Name.Should().Be(name);
            response.Headers.Location!.AbsolutePath.Should().BeEquivalentTo($"/api/genres/{created.Id}");

            (await GetGenreAsync(created.Id)).Should().BeEquivalentTo(created);
            (await GetAllGenresAsync()).Should().ContainEquivalentOf(created);
        }

        [Fact]
        public async Task Create_NameOfMaximumLength_ReturnsCreated()
        {
            var user = await RegisterUserAsync();
            var name = UniqueToken().PadRight(100, 'x');

            var response = await user.Client.PostAsJsonAsync("/api/genres", new CreateGenreDto { Name = name });

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            (await ReadAsAsync<GenreResponseDto>(response)).Name.Should().Be(name);
        }

        [Fact]
        public async Task Create_EmptyName_ReturnsBadRequest()
        {
            var user = await RegisterUserAsync();

            var response = await user.Client.PostAsJsonAsync("/api/genres", new CreateGenreDto { Name = "" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Create_NameLongerThanMaximumLength_ReturnsBadRequestAndCreatesNothing()
        {
            var user = await RegisterUserAsync();
            var name = UniqueToken().PadRight(101, 'x');

            var response = await user.Client.PostAsJsonAsync("/api/genres", new CreateGenreDto { Name = name });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await GetAllGenresAsync()).Should().NotContain(g => g.Name == name);
        }

        [Fact]
        public async Task Update_ExistingGenre_ReturnsNoContentAndRenamesIt()
        {
            var user = await RegisterUserAsync();
            var genre = await CreateGenreAsync(user, $"Before {UniqueToken()}");
            var newName = $"After {UniqueToken()}";

            var response = await user.Client.PutAsJsonAsync($"/api/genres/{genre.Id}", new UpdateGenreDto { Name = newName });

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await GetGenreAsync(genre.Id)).Should().BeEquivalentTo(new GenreResponseDto { Id = genre.Id, Name = newName });
        }

        [Fact]
        public async Task Update_NonexistentGenre_ReturnsNotFound()
        {
            var user = await RegisterUserAsync();

            var response = await user.Client.PutAsJsonAsync(
                $"/api/genres/{int.MaxValue}", new UpdateGenreDto { Name = $"Genre {UniqueToken()}" });

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Update_EmptyName_ReturnsBadRequestAndLeavesNameUnchanged()
        {
            var user = await RegisterUserAsync();
            var genre = await CreateGenreAsync(user, $"Genre {UniqueToken()}");

            var response = await user.Client.PutAsJsonAsync($"/api/genres/{genre.Id}", new UpdateGenreDto { Name = "" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await GetGenreAsync(genre.Id)).Name.Should().Be(genre.Name);
        }

        [Fact]
        public async Task Update_NameLongerThanMaximumLength_ReturnsBadRequestAndLeavesNameUnchanged()
        {
            var user = await RegisterUserAsync();
            var genre = await CreateGenreAsync(user, $"Genre {UniqueToken()}");

            var response = await user.Client.PutAsJsonAsync(
                $"/api/genres/{genre.Id}", new UpdateGenreDto { Name = new string('x', 101) });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await GetGenreAsync(genre.Id)).Name.Should().Be(genre.Name);
        }

        [Fact]
        public async Task Delete_ExistingGenre_ReturnsNoContentAndRemovesIt()
        {
            var user = await RegisterUserAsync();
            var genre = await CreateGenreAsync(user, $"Genre {UniqueToken()}");

            var response = await user.Client.DeleteAsync($"/api/genres/{genre.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await CreateClient().GetAsync($"/api/genres/{genre.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await GetAllGenresAsync()).Should().NotContain(g => g.Id == genre.Id);
        }

        [Fact]
        public async Task Delete_NonexistentGenre_ReturnsNotFound()
        {
            var user = await RegisterUserAsync();

            var response = await user.Client.DeleteAsync($"/api/genres/{int.MaxValue}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Delete_GenreAssignedToMovie_ReturnsNoContentAndRemovesItFromMovie()
        {
            var user = await RegisterUserAsync();
            var genre = await CreateGenreAsync(user, $"Genre {UniqueToken()}");
            var movie = await CreateMovieAsync(user, $"Movie {UniqueToken()}", genreIds: [genre.Id, DramaGenreId]);

            var response = await user.Client.DeleteAsync($"/api/genres/{genre.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            var details = (await CreateClient().GetFromJsonAsync<MovieDetailsResponseDto>($"/api/movies/{movie.Id}"))!;
            details.Movie.Genres.Should().ContainSingle().Which.Id.Should().Be(DramaGenreId);
        }

        private static async Task<GenreResponseDto> CreateGenreAsync(TestUser user, string name)
        {
            var response = await user.Client.PostAsJsonAsync("/api/genres", new CreateGenreDto { Name = name });
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            return await ReadAsAsync<GenreResponseDto>(response);
        }

        private async Task<GenreResponseDto> GetGenreAsync(int id)
        {
            return (await CreateClient().GetFromJsonAsync<GenreResponseDto>($"/api/genres/{id}"))!;
        }

        private async Task<List<GenreResponseDto>> GetAllGenresAsync()
        {
            return (await CreateClient().GetFromJsonAsync<List<GenreResponseDto>>("/api/genres"))!;
        }
    }
}
