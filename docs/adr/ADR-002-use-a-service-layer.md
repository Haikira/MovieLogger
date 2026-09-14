# ADR-002: Use a Service layer

## Status
Accepted

## Context
Business logic (validation, orchestration of repository calls, mapping between entities and DTOs) needs a home that is separate from both the HTTP-specific concerns of the API layer and the persistence-specific concerns of the data access layer. Putting this logic directly in controllers or repositories would couple business rules to HTTP or to EF Core, making both harder to test and change independently.

## Decision
Introduce a dedicated `MovieLogger.Service` project that holds business logic, domain entities, DTOs, repository interfaces, and AutoMapper mapping profiles (see ADR-009). Services such as `MovieService` and `GenreService` implement interfaces (`IMovieService`, `IGenreService`) that controllers in `MovieLogger.Api` depend on exclusively — controllers never call repositories or EF Core directly.

## Consequences
- Business logic can be unit-tested in isolation (`MovieLogger.Service.Tests`) without spinning up ASP.NET Core or a real database.
- The API layer stays a thin HTTP adapter; the DAL stays a thin persistence adapter; the Service layer is the single place business rules live.
- Adds an extra layer of indirection (interfaces + DI registrations) for what might otherwise be simple CRUD, which is an accepted cost for testability and separation of concerns.
- Related: ADR-008 (repository abstractions are kept in this layer, not in the DAL or API).
