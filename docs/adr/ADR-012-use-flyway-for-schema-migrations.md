# ADR-012: Use Flyway for schema migrations

## Status
Accepted

## Context
Schema creation, schema changes, and migration history tracking were previously handled by EF Core migrations (`MovieLogger.DAL/Migrations/`, generated and applied via `dotnet ef`). EF Core migrations are C# classes tied to the DbContext's model snapshot and to EF's own tooling and versioning — the generated SQL is a byproduct of diffing that model, not something authored and reviewed directly. As the project has moved onto SQL Server (see the SQL Server migration commit; a dedicated ADR for that move does not yet exist), it's a natural point to separate "how the schema gets built and evolves" from "how the app talks to that schema."

## Decision
Use Flyway as the sole mechanism for creating the database schema, applying schema changes, and tracking migration history, via plain, version-numbered SQL Server T-SQL files under `database/migrations/` (`V1__create_initial_schema.sql`, `V2__seed_genres.sql`, and so on). Flyway's schema history table becomes the authoritative record of what has been applied to a given database.

EF Core's role narrows to what it does inside `MovieLogger.DAL`: `MovieLoggerDbContext`, entity type configuration (`Configurations/`), querying and persisting data, and the repositories built on top of it. EF Core no longer owns schema creation or evolution — its `Migrations/` folder, the `IDesignTimeDbContextFactory` used only by `dotnet ef`, and the `Microsoft.EntityFrameworkCore.Design`/`Microsoft.Extensions.Configuration.*` package references that only existed to support that design-time tooling are removed. `MovieLoggerDbContext`'s model (via `OnModelCreating` and the `IEntityTypeConfiguration<T>` classes) must now be kept in sync with the Flyway-managed schema by hand, rather than being diffed automatically by EF's migration generator.

The existing SQL Server database is disposable, so no attempt was made to preserve EF's migration history or convert it into Flyway's history table — `V1__create_initial_schema.sql` recreates the schema EF's migration produced, from scratch.

## Consequences
- Every schema change is authored as reviewable, explicit T-SQL rather than generated from a C# model diff — full control over DDL (constraint names, index choices, data types), at the cost of writing it by hand.
- Flyway's schema history table is the single source of truth for what's been applied to a database; EF Core no longer has (or needs) any opinion about migration state.
- `MovieLoggerDbContext`'s configuration and the actual database schema are no longer mechanically guaranteed to agree — a schema change made only in a Flyway migration, or only in an `IEntityTypeConfiguration<T>`, will silently drift until someone reconciles the two. This is a tradeoff accepted for the reviewability and database-portability benefits of plain SQL.
- Running or resetting the local database now requires the Flyway CLI in addition to .NET tooling; see `docs/migrations.md` for local setup, including the Windows Integrated Auth caveat for connecting from Flyway's JDBC-based tooling.
- Seed data (e.g. the standard genre list in `V2__seed_genres.sql`) is now expressed as an idempotent Flyway migration rather than EF's `HasData` model seeding.
