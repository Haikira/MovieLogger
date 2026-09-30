namespace MovieLogger.Api.Tests.TestSupport
{
    /// <summary>A registered user, with an HTTP client that sends that user's JWT on every request.</summary>
    public record TestUser(int Id, string Email, string Token, HttpClient Client);
}
