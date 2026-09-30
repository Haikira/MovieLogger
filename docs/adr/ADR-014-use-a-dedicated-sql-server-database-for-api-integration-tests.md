# ADR-014: Use a dedicated SQL Server database for API integration tests

## Status
Accepted

## Context
`MovieLogger.Service.Tests` unit-tests the service layer with NSubstitute fakes for the repositories, so nothing automated exercised the HTTP pipeline end to end: JWT validation, `[Authorize]`, model validation, controllers deriving the user id from the token, and the EF Core queries that enforce per-user isolation against a real schema. That behaviour had only been checked by hand in Postman.

Integration tests need a database. The options were EF Core's in-memory provider, SQLite, or SQL Server itself. The in-memory provider and SQLite don't behave like SQL Server (no check constraints on the in-memory provider, different string comparison and translation rules, different identity/date behaviour), and since Flyway rather than EF Core owns the schema ([ADR-012](ADR-012-use-flyway-for-schema-migrations.md)), EF's `EnsureCreated` would build a schema from the C# model instead of the one production actually runs on. Pointing the tests at the development database (`MovieLoggerDb`) would make them depend on, and pollute, whatever data happens to be there.

## Decision
`MovieLogger.Api.Tests` hosts the real application in memory with `WebApplicationFactory<Program>` and runs it against a dedicated SQL Server database, `MovieLoggerDb_ApiTests`, on the same local instance used for development:

- At the start of a test run the database is dropped (if a previous run left it behind) and recreated, and the Flyway migration scripts in `database/migrations/` are applied to it in version order, split on `GO` batch separators as Flyway does. The scripts are linked into the test project's output, so the test schema always comes from the same files as every other environment. The Flyway CLI isn't needed to run the tests.
- At the end of the run the database is dropped.
- The factory overrides only `ConnectionStrings:MovieLoggerDb` and `Jwt:Key` (a random key generated per run) and uses a `Testing` environment, so the developer's user secrets aren't loaded. On startup it checks that the application's `DbContext` points at `MovieLoggerDb_ApiTests` and fails fast otherwise.
- All test classes share one factory and database through an xUnit collection fixture. Tests don't reset data between each other; each test registers its own users and creates its own uniquely named movies, so tests are independent of each other and of execution order.
- Users obtain tokens through the real `/api/auth/register` and `/api/auth/login` endpoints rather than through test-only authentication handlers, so JWT issuance and validation are both exercised.

## Consequences
- Tests run against the same schema, constraints and query translation that production uses, and exercise the real authentication and authorization configuration.
- Running the API tests requires a reachable SQL Server instance whose login can create and drop databases. By default that's `localhost` with Windows Integrated Authentication; `MOVIELOGGER_TEST_SQLSERVER` can supply a different server-level connection string (for example in CI).
- The tests run sequentially within one collection and share a single database, so a test must never assert on global counts (e.g. "all movies"). It has to scope its assertions to data it created itself, for example by searching on a unique title token.
- Flyway's schema history table isn't created in the test database, because the scripts are applied directly rather than through the Flyway CLI. Only versioned (`V{n}__*.sql`) migrations are applied; if repeatable or callback migrations are ever introduced, `TestDatabase` will need to support them.
- Two test runs executing at the same moment against the same SQL Server instance would collide on the fixed database name. That's acceptable for local development and a single CI job.
