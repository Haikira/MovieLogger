# ADR-003: Use Entity Framework Core

## Status
Accepted

## Context
MovieLogger needs to persist and query relational data (movies, genres, and the relationship between them) from .NET code. The team wanted a mature, well-supported ORM with first-class .NET/ASP.NET Core integration, migration tooling, and support for multiple database providers so the same data access code could target different databases across environments.

## Decision
Use Entity Framework Core as the ORM, isolated inside the `MovieLogger.DAL` project. `MovieLoggerDbContext` (`MovieLogger.DAL\MovieLoggerDbContext.cs`) defines the model, entity configurations, and change tracking; schema changes are managed through EF Core migrations under `MovieLogger.DAL\Migrations\` (e.g. `20260910135636_InitialCreate.cs`). The project references `Microsoft.EntityFrameworkCore.Sqlite` and `Microsoft.EntityFrameworkCore.Design` for the current provider and design-time tooling.

## Consequences
- Gets code-first modeling, LINQ-based querying, change tracking, and migration generation/versioning out of the box.
- Provider is swappable in principle (EF Core abstracts SQL generation per provider); the current concrete choice is SQLite (see ADR-004).
- Ties the DAL to EF Core's API and migration model — schema changes must go through migrations rather than ad-hoc SQL scripts.
- EF Core usage is confined to `MovieLogger.DAL`; the Service and Api projects interact with data only through repository interfaces (see ADR-005), so EF Core could be swapped for another data access technology without touching business logic or controllers.
