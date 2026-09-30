# Testing

The backend has two test projects, both part of `MovieLogger.slnx`:

| Project | What it tests | Dependencies |
| --- | --- | --- |
| `server/test/MovieLogger.Service.Tests` | Service-layer business logic, with repositories faked by NSubstitute | None |
| `server/test/MovieLogger.Api.Tests` | The HTTP API end to end: routing, model validation, JWT authentication/authorization, controllers, services, repositories and EF Core against SQL Server | A local SQL Server instance |

Run everything from the repository root:

```
dotnet test MovieLogger.slnx
```

Or run one project:

```
dotnet test server/test/MovieLogger.Service.Tests
dotnet test server/test/MovieLogger.Api.Tests
```

## API integration tests

`MovieLogger.Api.Tests` hosts the real application in memory with `WebApplicationFactory<Program>` and sends real HTTP requests to it. See [ADR-014](adr/ADR-014-use-a-dedicated-sql-server-database-for-api-integration-tests.md) for why it works this way.

### Local setup

No extra setup is needed beyond the SQL Server instance you already use for development:

- The tests connect to `localhost` with Windows Integrated Authentication (`Trusted_Connection=True;TrustServerCertificate=True`), the same as `appsettings.json`.
- Your Windows login needs permission to create and drop databases on that instance. A local administrator/`sysadmin` login, which is the default for a local Developer Edition install, has it.
- You **don't** need the Flyway CLI, a user secret, or a `Jwt:Key` to run the tests. The tests generate their own signing key for each run and never load your user secrets.

To use a different SQL Server (for example in CI, or a named instance), set `MOVIELOGGER_TEST_SQLSERVER` to a connection string without a database name:

```
$env:MOVIELOGGER_TEST_SQLSERVER = "Server=localhost\SQLEXPRESS;Trusted_Connection=True;TrustServerCertificate=True;"
```

Don't commit connection strings that contain passwords. Supply them through the environment instead.

### The test database

- The tests use their own database, `MovieLoggerDb_ApiTests`. They never read from or write to `MovieLoggerDb`, and the test factory fails fast if the application ends up configured with any other database.
- At the start of each run the database is dropped (if it exists) and recreated, and every `database/migrations/V{n}__*.sql` script is applied in version order. The schema is therefore always the current Flyway schema, including the seeded genres.
- At the end of the run the database is dropped. If a run is killed part-way through, the database may be left behind; the next run drops and recreates it, or you can drop it manually.
- Tests don't depend on each other or on execution order. Each test registers its own users (unique email addresses) and creates its own movies. A test that searches shared data, such as the movie catalogue, uses a unique title token so it only sees rows it created itself.

### Structure

- `TestSupport/MovieLoggerApiFactory.cs`: the `WebApplicationFactory`, which provisions the database and overrides the connection string and JWT key.
- `TestSupport/TestDatabase.cs`: creates, migrates and drops `MovieLoggerDb_ApiTests`.
- `TestSupport/ApiTestCollection.cs`: the xUnit collection that shares one factory/database across all test classes.
- `TestSupport/ApiTestBase.cs`: helpers to register a user (getting a real JWT from the API), create authenticated clients, create movies and log watches.
- `Controllers/*Tests.cs`: one class per controller or feature.

New API test classes should derive from `ApiTestBase`, carry `[Collection(ApiTestCollection.Name)]`, and create all the data they rely on.
