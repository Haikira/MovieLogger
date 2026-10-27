using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MovieLogger.Api.Tests.TestSupport;

namespace MovieLogger.Api.Tests.Hosting
{
    /// <summary>
    /// Per-client-IP rate limits, hosted with small limits. The requests are chosen so they never reach the
    /// database: an empty login body fails model validation (400) and an unknown path is a 404. Both still
    /// count against the limits, because the rate limiter runs before the endpoint.
    /// </summary>
    public class RateLimitingTests
    {
        // The CloudFront edge address, which the load balancer appends after the client address CloudFront added.
        private const string EdgeAddress = "203.0.113.5";

        [Fact]
        public async Task Login_OverTheLimit_ReturnsTooManyRequestsWithRetryAfter()
        {
            using var factory = CreateFactory(loginAttempts: 2);
            var client = factory.CreateClient();

            (await LoginAsync(client)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await LoginAsync(client)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var rejected = await LoginAsync(client);

            rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
            rejected.Headers.RetryAfter!.Delta.Should().BePositive();
            (await rejected.Content.ReadAsStringAsync()).Should().Contain("Too many requests");
        }

        [Fact]
        public async Task Register_OverTheLimit_ReturnsTooManyRequests()
        {
            using var factory = CreateFactory(registrations: 1);
            var client = factory.CreateClient();

            (await client.PostAsJsonAsync("/api/auth/register", new { })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await client.PostAsJsonAsync("/api/auth/register", new { })).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        }

        [Fact]
        public async Task AnyEndpoint_OverTheGlobalLimit_ReturnsTooManyRequests()
        {
            using var factory = CreateFactory(requestsPerMinute: 2);
            var client = factory.CreateClient();

            (await client.GetAsync("/no-such-endpoint")).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await client.GetAsync("/no-such-endpoint")).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await client.GetAsync("/no-such-endpoint")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        }

        [Fact]
        public async Task Health_IsNeverRateLimited()
        {
            using var factory = CreateFactory(requestsPerMinute: 1);
            var client = factory.CreateClient();

            for (var i = 0; i < 3; i++)
            {
                (await client.GetAsync("/health")).StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        [Fact]
        public async Task Limits_BehindTwoProxies_ApplyPerClientNotPerEdgeAddress()
        {
            using var factory = CreateFactory(loginAttempts: 1, forwardLimit: 2);
            var client = factory.CreateClient();

            // Two clients arriving through the same CloudFront edge each get their own allowance...
            (await LoginAsync(client, $"198.51.100.1, {EdgeAddress}")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await LoginAsync(client, $"198.51.100.2, {EdgeAddress}")).StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // ...and a client can't escape its limit by sending its own X-Forwarded-For entry, because that
            // sits to the left of the address CloudFront added.
            (await LoginAsync(client, $"192.0.2.99, 198.51.100.1, {EdgeAddress}")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        }

        [Fact]
        public async Task Register_WhenRegistrationIsDisabled_ReturnsForbiddenWithMessage()
        {
            var settings = ConfiguredApiFactory.ValidSettings();
            settings["Registration:Enabled"] = "false";
            using var factory = new ConfiguredApiFactory(settings);

            var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", new
            {
                DisplayName = "Closed Out",
                Email = "closed-out@movielogger.test",
                Password = "Correct-Horse-9",
                ConfirmPassword = "Correct-Horse-9"
            });

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await response.Content.ReadAsStringAsync()).Should().Contain("Registration is currently closed");
        }

        private static ConfiguredApiFactory CreateFactory(
            int requestsPerMinute = 100, int loginAttempts = 100, int registrations = 100, int? forwardLimit = null)
        {
            var settings = ConfiguredApiFactory.ValidSettings();
            settings["RateLimiting:RequestsPerMinute"] = requestsPerMinute.ToString();
            settings["RateLimiting:LoginAttemptsPerFiveMinutes"] = loginAttempts.ToString();
            settings["RateLimiting:RegistrationsPerHour"] = registrations.ToString();
            if (forwardLimit.HasValue)
            {
                settings["ForwardedHeaders:ForwardLimit"] = forwardLimit.Value.ToString();
            }

            return new ConfiguredApiFactory(settings);
        }

        private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string? forwardedFor = null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login") { Content = JsonContent.Create(new { }) };
            if (forwardedFor is not null)
            {
                request.Headers.Add("X-Forwarded-For", forwardedFor);
            }

            return await client.SendAsync(request);
        }
    }
}
