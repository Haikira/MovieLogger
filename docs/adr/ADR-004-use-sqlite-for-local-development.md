# ADR-004: Use SQLite for local development

## Status
Accepted

## Context
The project needs a concrete database provider to run against during development. It should require no external database server installation or configuration so that any contributor can clone the repo and run the API immediately, with EF Core migrations able to create the schema on demand.

## Decision
Use SQLite, via the `Microsoft.EntityFrameworkCore.Sqlite` package in `MovieLogger.DAL`. The provider is registered with `options.UseSqlite(...)` in `MovieLogger.DAL\Extensions\ServiceCollectionExtensions.cs`, using the connection string `"Data Source=movielogger.db"` defined under `ConnectionStrings:MovieLoggerDb` in `MovieLogger.Api\appsettings.json`. `appsettings.Development.json` has no override, so this same file-based SQLite database is currently used for local development.

## Consequences
- Zero-setup local development: no database server to install, configure, or run — the `.db` file is created locally from migrations.
- Because EF Core abstracts most provider differences, application/business code (Service layer, repositories) is not coupled to SQLite specifically.
- SQLite has known behavioral differences from server-grade databases (e.g. type affinity, limited concurrent-write support, some LINQ translations differ) — features validated only against SQLite may behave differently on another provider.
- Since `appsettings.Development.json` currently has no override, local development and any future non-local environment must be deliberately configured with different connection strings/providers before this stops being a shared dev/prod database — this is a known forward-looking gap, not yet addressed.
