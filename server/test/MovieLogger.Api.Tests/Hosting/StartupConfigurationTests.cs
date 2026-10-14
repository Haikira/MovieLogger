using FluentAssertions;
using MovieLogger.Api.Tests.TestSupport;

namespace MovieLogger.Api.Tests.Hosting
{
    /// <summary>
    /// The API must refuse to start when required configuration is missing or invalid, rather than
    /// failing later on the first request that needs it.
    /// </summary>
    public class StartupConfigurationTests
    {
        [Fact]
        public void Startup_WithValidSettings_Succeeds()
        {
            using var factory = new ConfiguredApiFactory(ConfiguredApiFactory.ValidSettings());

            var act = () => factory.CreateClient();

            act.Should().NotThrow();
        }

        [Fact]
        public void Startup_WithoutConnectionString_Fails()
        {
            var settings = ConfiguredApiFactory.ValidSettings();
            settings.Remove("ConnectionStrings:MovieLoggerDb");

            StartupFailure(settings).WithMessage("ConnectionStrings:MovieLoggerDb is not configured*");
        }

        [Fact]
        public void Startup_WithoutJwtKey_Fails()
        {
            var settings = ConfiguredApiFactory.ValidSettings();
            settings.Remove("Jwt:Key");

            StartupFailure(settings).WithMessage("Jwt:Key is not configured*");
        }

        [Fact]
        public void Startup_WithJwtKeyShorterThan32Bytes_FailsWithoutRevealingTheKey()
        {
            var settings = ConfiguredApiFactory.ValidSettings();
            var shortKey = new string('k', 31);
            settings["Jwt:Key"] = shortKey;

            var failure = StartupFailure(settings).WithMessage("Jwt:Key is too short*");

            failure.Which.Message.Should().NotContain(shortKey);
        }

        [Fact]
        public void Startup_WithJwtKeyOfExactly32Bytes_Succeeds()
        {
            var settings = ConfiguredApiFactory.ValidSettings();
            settings["Jwt:Key"] = new string('k', 32);
            using var factory = new ConfiguredApiFactory(settings);

            var act = () => factory.CreateClient();

            act.Should().NotThrow();
        }

        [Theory]
        [InlineData("https://app.example.com/")]
        [InlineData("https://app.example.com/path")]
        [InlineData("app.example.com")]
        [InlineData("ftp://app.example.com")]
        public void Startup_WithCorsAllowedOriginThatIsNotAnOrigin_Fails(string origin)
        {
            var settings = ConfiguredApiFactory.ValidSettings();
            settings["Cors:AllowedOrigins:0"] = origin;

            StartupFailure(settings).WithMessage("Cors:AllowedOrigins contains*");
        }

        private static FluentAssertions.Specialized.ExceptionAssertions<InvalidOperationException> StartupFailure(
            IReadOnlyDictionary<string, string> settings)
        {
            using var factory = new ConfiguredApiFactory(settings);

            var act = () => factory.CreateClient();

            return act.Should().Throw<InvalidOperationException>();
        }
    }
}
