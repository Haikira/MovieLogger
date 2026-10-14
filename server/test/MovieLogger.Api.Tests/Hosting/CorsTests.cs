using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using MovieLogger.Api.Tests.TestSupport;

namespace MovieLogger.Api.Tests.Hosting
{
    public class CorsTests : IDisposable
    {
        private const string AllowedOrigin = "https://app.movielogger.test";
        private const string DisallowedOrigin = "https://evil.movielogger.test";

        private readonly ConfiguredApiFactory _factory;

        public CorsTests()
        {
            var settings = ConfiguredApiFactory.ValidSettings();
            settings["Cors:AllowedOrigins:0"] = AllowedOrigin;
            _factory = new ConfiguredApiFactory(settings);
        }

        public void Dispose() => _factory.Dispose();

        [Fact]
        public async Task Request_FromAllowedOrigin_ReceivesCorsHeaders()
        {
            var response = await SendAsync(HttpMethod.Get, "/health", AllowedOrigin);

            response.Headers.GetValues("Access-Control-Allow-Origin").Should().ContainSingle().Which.Should().Be(AllowedOrigin);
        }

        [Fact]
        public async Task Request_FromDisallowedOrigin_DoesNotReceiveCorsPermission()
        {
            var response = await SendAsync(HttpMethod.Get, "/health", DisallowedOrigin);

            response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
        }

        [Fact]
        public async Task Preflight_FromAllowedOrigin_PermitsAuthenticatedJsonRequests()
        {
            var response = await SendPreflightAsync(AllowedOrigin);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            response.Headers.GetValues("Access-Control-Allow-Origin").Should().ContainSingle().Which.Should().Be(AllowedOrigin);
            response.Headers.GetValues("Access-Control-Allow-Methods").Should().Contain(value => value.Contains("POST"));
            var allowedHeaders = string.Join(",", response.Headers.GetValues("Access-Control-Allow-Headers")).ToLowerInvariant();
            allowedHeaders.Should().Contain("authorization").And.Contain("content-type");
        }

        [Fact]
        public async Task Preflight_FromAllowedOrigin_DoesNotAllowCredentials()
        {
            var response = await SendPreflightAsync(AllowedOrigin);

            response.Headers.Contains("Access-Control-Allow-Credentials").Should().BeFalse();
        }

        [Fact]
        public async Task Preflight_FromDisallowedOrigin_DoesNotReceiveCorsPermission()
        {
            var response = await SendPreflightAsync(DisallowedOrigin);

            response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
        }

        [Fact]
        public async Task Request_WhenNoOriginsAreConfigured_DoesNotReceiveCorsHeaders()
        {
            using var factory = new ConfiguredApiFactory(ConfiguredApiFactory.ValidSettings());
            using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
            request.Headers.Add("Origin", AllowedOrigin);

            var response = await factory.CreateClient().SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
        }

        private Task<HttpResponseMessage> SendPreflightAsync(string origin) =>
            SendAsync(HttpMethod.Options, "/api/movies", origin, request =>
            {
                request.Headers.Add("Access-Control-Request-Method", "POST");
                request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");
            });

        private async Task<HttpResponseMessage> SendAsync(
            HttpMethod method, string path, string origin, Action<HttpRequestMessage>? configure = null)
        {
            using var request = new HttpRequestMessage(method, path);
            request.Headers.Add("Origin", origin);
            configure?.Invoke(request);

            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            return await client.SendAsync(request);
        }
    }
}
