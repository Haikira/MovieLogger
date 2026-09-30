using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MovieLogger.DAL;

namespace MovieLogger.Api.Tests.TestSupport
{
    /// <summary>
    /// Hosts the real MovieLogger.Api application (Program.cs, middleware, JWT authentication, controllers,
    /// services, repositories and EF Core) in memory, pointed at a dedicated SQL Server test database.
    /// </summary>
    public class MovieLoggerApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly TestDatabase _database = new();

        // Generated per test run so no signing key is ever committed or shared with a real environment.
        private readonly string _jwtKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // A non-Development environment means the developer's user secrets (including their local
            // Jwt:Key) aren't loaded, so the tests only depend on the settings supplied here.
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:MovieLoggerDb", _database.ConnectionString);
            builder.UseSetting("Jwt:Key", _jwtKey);
        }

        public async Task InitializeAsync()
        {
            await _database.CreateAsync();
            EnsureApplicationUsesTestDatabase();
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await base.DisposeAsync();
            await _database.DropAsync();
        }

        // Guards against a configuration change silently pointing the tests at the development database.
        private void EnsureApplicationUsesTestDatabase()
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MovieLoggerDbContext>();
            var databaseName = dbContext.Database.GetDbConnection().Database;

            if (databaseName != TestDatabase.DatabaseName)
            {
                throw new InvalidOperationException(
                    $"The API is configured to use database '{databaseName}' instead of '{TestDatabase.DatabaseName}'.");
            }
        }
    }
}
