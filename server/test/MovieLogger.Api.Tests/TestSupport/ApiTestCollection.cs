namespace MovieLogger.Api.Tests.TestSupport
{
    /// <summary>
    /// Shares one <see cref="MovieLoggerApiFactory"/> (and therefore one test database) across every API
    /// test class. Tests don't rely on each other's data: each creates its own uniquely-named users and
    /// movies, so they pass in any order.
    /// </summary>
    [CollectionDefinition(Name)]
    public class ApiTestCollection : ICollectionFixture<MovieLoggerApiFactory>
    {
        public const string Name = "MovieLogger API";
    }
}
