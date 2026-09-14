# ADR-005: Use the Repository Pattern

## Status
Accepted

## Context
The Service layer needs to read and write data without being coupled directly to EF Core or `MovieLoggerDbContext`. Calling EF Core directly from services would make it hard to unit-test business logic without a real (or in-memory) database, and would spread persistence details (DbSets, `Include` calls, query composition) throughout the business logic.

## Decision
Adopt the Repository pattern: persistence access is expressed through repository interfaces owned by the Service layer, with concrete implementations living in the DAL. This was introduced via commit `b280aab` ("Refactor repositories with generic base", merged in PR #1 / `4ce8441`), which established a generic base repository plus entity-specific repositories on top of it (see ADR-006 and ADR-007).

## Consequences
- Services depend on abstractions (`IRepository<T>`, `IMovieRepository`, `IGenreRepository`), not on EF Core or `MovieLoggerDbContext`, so business logic can be unit-tested with fakes/mocks of the repositories.
- Persistence details (querying, `Include`s, `DbSet` access) are centralized in `MovieLogger.DAL\Repositories\`, keeping the Service layer free of EF Core-specific code.
- Adds an abstraction layer between services and EF Core; simple pass-through CRUD calls have an extra indirection cost, which is offset by the generic repository (ADR-006) so it isn't repeated per entity.
