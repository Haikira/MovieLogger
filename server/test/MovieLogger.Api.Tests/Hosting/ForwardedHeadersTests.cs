using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using MovieLogger.Api.Tests.TestSupport;

namespace MovieLogger.Api.Tests.Hosting
{
    /// <summary>
    /// Behind a load balancer that terminates TLS, the API receives plain HTTP plus X-Forwarded-Proto.
    /// With an HTTPS port configured, HTTPS redirection must treat a forwarded HTTPS request as HTTPS
    /// (no redirect loop) while still redirecting genuine HTTP requests.
    /// </summary>
    public class ForwardedHeadersTests : IDisposable
    {
        private readonly ConfiguredApiFactory _factory;

        public ForwardedHeadersTests()
        {
            var settings = ConfiguredApiFactory.ValidSettings();
            settings["https_port"] = "443";
            _factory = new ConfiguredApiFactory(settings);
        }

        public void Dispose() => _factory.Dispose();

        [Fact]
        public async Task Request_ForwardedAsHttps_IsNotRedirected()
        {
            var response = await SendAsync(forwardedProto: "https");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Request_ForwardedAsHttp_IsRedirectedToHttps()
        {
            var response = await SendAsync(forwardedProto: "http");

            response.StatusCode.Should().Be(HttpStatusCode.TemporaryRedirect);
            response.Headers.Location!.Scheme.Should().Be("https");
        }

        [Fact]
        public async Task Request_WithoutForwardedHeaders_IsRedirectedToHttps()
        {
            var response = await SendAsync(forwardedProto: null);

            response.StatusCode.Should().Be(HttpStatusCode.TemporaryRedirect);
            response.Headers.Location!.Scheme.Should().Be("https");
        }

        private async Task<HttpResponseMessage> SendAsync(string? forwardedProto)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
            if (forwardedProto is not null)
            {
                request.Headers.Add("X-Forwarded-For", "203.0.113.10");
                request.Headers.Add("X-Forwarded-Proto", forwardedProto);
            }

            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            return await client.SendAsync(request);
        }
    }
}
