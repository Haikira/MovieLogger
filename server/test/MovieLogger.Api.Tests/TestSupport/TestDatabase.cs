using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace MovieLogger.Api.Tests.TestSupport
{
    /// <summary>
    /// A throwaway SQL Server database for the API integration tests. It is dropped and recreated at the
    /// start of each test run and dropped again at the end, so the tests never touch MovieLoggerDb and
    /// never depend on data left behind by a previous run.
    /// </summary>
    /// <remarks>
    /// The schema is built by applying the Flyway migration scripts from <c>database/migrations</c>
    /// (copied into the test output) in version order, splitting each script on <c>GO</c> batch
    /// separators the same way Flyway does. The Flyway CLI itself isn't required to run the tests.
    /// </remarks>
    public sealed partial class TestDatabase
    {
        public const string DatabaseName = "MovieLoggerDb_ApiTests";

        /// <summary>
        /// Optional environment variable holding a SQL Server connection string (without a database) to
        /// use instead of the default local instance with Windows Integrated Authentication.
        /// </summary>
        public const string ServerConnectionStringVariable = "MOVIELOGGER_TEST_SQLSERVER";

        private const string DefaultServerConnectionString = "Server=localhost;Trusted_Connection=True;TrustServerCertificate=True;";

        private readonly string _masterConnectionString;

        public TestDatabase()
        {
            var serverConnectionString = Environment.GetEnvironmentVariable(ServerConnectionStringVariable);
            var builder = new SqlConnectionStringBuilder(
                string.IsNullOrWhiteSpace(serverConnectionString) ? DefaultServerConnectionString : serverConnectionString);

            builder.InitialCatalog = "master";
            _masterConnectionString = builder.ConnectionString;

            builder.InitialCatalog = DatabaseName;
            ConnectionString = builder.ConnectionString;
        }

        public string ConnectionString { get; }

        public async Task CreateAsync()
        {
            await DropAsync();
            await ExecuteAsync(_masterConnectionString, [$"CREATE DATABASE [{DatabaseName}];"]);

            foreach (var script in GetMigrationScripts())
            {
                var sql = await File.ReadAllTextAsync(script);
                await ExecuteAsync(ConnectionString, SplitIntoBatches(sql));
            }
        }

        public async Task DropAsync()
        {
            SqlConnection.ClearAllPools();

            await ExecuteAsync(_masterConnectionString,
            [
                $"""
                IF DB_ID(N'{DatabaseName}') IS NOT NULL
                BEGIN
                    ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{DatabaseName}];
                END
                """
            ]);
        }

        private static IEnumerable<string> GetMigrationScripts()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "migrations");
            var scripts = Directory.GetFiles(directory, "V*__*.sql")
                .Select(path => (Path: path, Match: VersionedMigrationFileName().Match(Path.GetFileName(path))))
                .Where(x => x.Match.Success)
                .OrderBy(x => int.Parse(x.Match.Groups["version"].Value))
                .Select(x => x.Path)
                .ToList();

            if (scripts.Count == 0)
            {
                throw new InvalidOperationException($"No Flyway migration scripts were found in '{directory}'.");
            }

            return scripts;
        }

        private static IEnumerable<string> SplitIntoBatches(string sql)
        {
            return BatchSeparator().Split(sql).Where(batch => !string.IsNullOrWhiteSpace(batch));
        }

        private static async Task ExecuteAsync(string connectionString, IEnumerable<string> batches)
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            foreach (var batch in batches)
            {
                await using var command = new SqlCommand(batch, connection);
                await command.ExecuteNonQueryAsync();
            }
        }

        [GeneratedRegex(@"^V(?<version>\d+)__.+\.sql$", RegexOptions.IgnoreCase)]
        private static partial Regex VersionedMigrationFileName();

        [GeneratedRegex(@"^\s*GO\s*;?\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
        private static partial Regex BatchSeparator();
    }
}
