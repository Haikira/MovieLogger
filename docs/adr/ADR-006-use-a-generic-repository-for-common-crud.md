# ADR-006: Use a generic repository for common CRUD

## Status
Accepted

## Context
Once the Repository pattern was adopted (ADR-005), each entity (Movie, Genre, and future entities) would otherwise need its own hand-written implementation of the same basic CRUD operations (get all, get by id, add, update, delete), duplicating near-identical EF Core code across repositories.

## Decision
Define a generic `IRepository<T>` interface (`server\src\MovieLogger.Service\Repositories\IRepository.cs`) with `GetAllAsync`, `GetByIdAsync`, `AddAsync`, `UpdateAsync`, and `DeleteAsync`, implemented once by `Repository<T>` (`server\src\MovieLogger.DAL\Repositories\Repository.cs`), which operates against `Context.Set<T>()` on `MovieLoggerDbContext`. Its CRUD methods are declared `virtual` so entity-specific repositories can override behavior where needed (see ADR-007). `IRepository<>` is registered as an open generic in DI (`server\src\MovieLogger.DAL\Extensions\ServiceCollectionExtensions.cs`), giving every entity a working repository with no extra code.

## Consequences
- Common CRUD logic is written and maintained in exactly one place instead of once per entity.
- New entities get a fully functional repository for free by simply declaring `IRepository<NewEntity>` (or a specialized interface extending it).
- The generic implementation only covers straightforward CRUD; anything entity-specific (eager loading, custom queries) must be added via specialized repositories (ADR-007) rather than growing the generic base with entity-aware conditionals.
