# ADR-007: Use specialised repositories for entity-specific queries

## Status
Accepted

## Context
The generic repository (ADR-006) only covers uniform CRUD operations. Some entities need queries the generic base cannot express generically — for example, `Movie` needs its `Genres` navigation property eagerly loaded on reads, and `Genre` needs a bulk lookup by a set of ids. Adding entity-aware branches to the generic `Repository<T>` would break its genericity and couple it to specific entity shapes.

## Decision
Define specialized repository interfaces that extend the generic one — `IMovieRepository : IRepository<Movie>` and `IGenreRepository : IRepository<Genre>` (`server\src\MovieLogger.Service\Repositories\`) — and implement them as `MovieRepository : Repository<Movie>, IMovieRepository` and `GenreRepository : Repository<Genre>, IGenreRepository` (`server\src\MovieLogger.DAL\Repositories\`). These override generic methods where needed (e.g. `MovieRepository` overrides `GetAllAsync`/`GetByIdAsync` to `.Include(m => m.Genres)`) and add entity-specific methods (e.g. `GenreRepository.GetByIdsAsync`). Each is registered in DI alongside the generic registration.

## Consequences
- Entity-specific query needs are met without polluting the generic `Repository<T>` with conditional, entity-aware logic.
- Services depend on the narrowest interface they need (`IMovieRepository`, `IGenreRepository`), which still exposes the full generic CRUD surface via inheritance.
- Every entity with non-trivial query needs requires its own interface/implementation pair, following the same override pattern — a small, consistent amount of boilerplate per entity in exchange for keeping the generic base simple.
- Related: ADR-006 (generic base these specialize) and ADR-005 (overall repository abstraction).
