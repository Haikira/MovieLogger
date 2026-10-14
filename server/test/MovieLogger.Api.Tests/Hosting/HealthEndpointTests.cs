using System.Net;
using FluentAssertions;
using MovieLogger.Api.Tests.TestSupport;

namespace MovieLogger.Api.Tests.Hosting
{
    [Collection(ApiTestCollection.Name)]
    public class HealthEndpointTests(MovieLoggerApiFactory factory) : ApiTestBase(factory)
    {
        [Fact]
        public async Task Health_WithoutAuthentication_ReturnsOk()
        {
            var response = await CreateClient().GetAsync("/health");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await response.Content.ReadAsStringAsync()).Should().Be("Healthy");
        }
    }
}
