using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MovieLogger.Api.Tests.TestSupport
{
    /// <summary>
    /// Hosts the real MovieLogger.Api application with exactly the settings given, for tests of startup
    /// configuration and middleware (CORS, forwarded headers, health) that never touch the database.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="MovieLoggerApiFactory"/>, no test database is created. The connection string in
    /// <see cref="ValidSettings"/> only satisfies the startup check; nothing ever opens it.
    /// </remarks>
    public sealed class ConfiguredApiFactory(IReadOnlyDictionary<string, string> settings) : WebApplicationFactory<Program>
    {
        /// <summary>The minimum configuration the API needs to start.</summary>
        public static Dictionary<string, string> ValidSettings() => new()
        {
            ["ConnectionStrings:MovieLoggerDb"] = "Server=unused.invalid;Database=NotOpenedByTheseTests;",
            // Generated per run so no signing key is ever committed or shared with a real environment.
            ["Jwt:Key"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
        };

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Not Development, so the developer's user secrets and appsettings.Development.json aren't loaded
            // and the app only sees the settings supplied here.
            builder.UseEnvironment("Testing");

            foreach (var (key, value) in settings)
            {
                builder.UseSetting(key, value);
            }
        }
    }
}
