# ADR-011: Use SQL Server for local development

## Status
Accepted. Supersedes [ADR-004](ADR-004-use-sqlite-for-local-development.md).

This ADR was written retrospectively. The change itself landed in commit `5ed7772` ("Migrate MovieLogger to SQL Server", September 2026).

## Context
ADR-004 chose SQLite so the API could run with no database server to install. Its own consequences named the cost: SQLite differs from server-grade databases (type affinity, limited concurrent writes, different LINQ translation), so behaviour validated only against SQLite might not hold on the database the app would actually run on, and dev and any deployed environment would have to be split onto different providers deliberately.

As the domain grew (per-user watch history, aggregates for My Movies and the dashboard, check constraints on ratings, cascading deletes), those differences mattered more. The project is also a vehicle for practising a realistic .NET stack, where SQL Server is the usual production choice.

## Decision
Use Microsoft SQL Server as the only database provider, in every environment including local development:

- `MovieLogger.DAL` registers `MovieLoggerDbContext` with `UseSqlServer(...)` (`server/src/MovieLogger.DAL/Extensions/ServiceCollectionExtensions.cs`) and references `Microsoft.EntityFrameworkCore.SqlServer`. The SQLite provider was removed.
- Local development uses the default SQL Server instance on `localhost` with Windows Integrated Authentication and a database named `MovieLoggerDb`, configured in `ConnectionStrings:MovieLoggerDb` in `server/src/MovieLogger.Api/appsettings.json`. `TrustServerCertificate=True` is set because a local instance uses a self-signed certificate.
- `database/setup/create-database.ps1` creates `MovieLoggerDb` if it doesn't exist. The schema itself is managed by Flyway ([ADR-012](ADR-012-use-flyway-for-schema-migrations.md)).

## Consequences
- Development, the API integration tests ([ADR-014](ADR-014-use-a-dedicated-sql-server-database-for-api-integration-tests.md)) and any future deployment run on the same engine, so query translation, constraints, collation (case-insensitive by default, which the unique email index relies on) and date types behave the same everywhere.
- Contributors need a local SQL Server instance (Developer or Express edition) before they can run the API or the integration tests. Setup is no longer zero-install.
- Windows Integrated Authentication keeps credentials out of the repository, but it ties the default configuration to Windows. Other platforms (or a SQL Server container) need a different connection string, supplied through user secrets or environment variables rather than committed.
- `TrustServerCertificate=True` is acceptable for a local instance only. A deployed environment needs a properly trusted certificate and its own connection string.
