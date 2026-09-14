# ADR-008: Keep repository abstractions in the Service layer

## Status
Accepted

## Context
Repository interfaces (`IRepository<T>`, `IMovieRepository`, `IGenreRepository`) need to live in one project, with implementations (`Repository<T>`, `MovieRepository`, `GenreRepository`) in another. Where the interfaces live determines which projects can depend on persistence abstractions without depending on EF Core or the DAL's concrete types.

## Decision
Define repository interfaces in `MovieLogger.Service\Repositories\`, not in `MovieLogger.DAL`. `MovieLogger.DAL` implements these interfaces but the interfaces themselves are owned by the Service layer, which is the only layer that consumes them directly. Confirmed in code: `MoviesController` (`MovieLogger.Api\Controllers\MoviesController.cs`) injects only `IMovieService`, never a repository type, and `MovieService` (`MovieLogger.Service\Services\MovieService.cs`) injects `IMovieRepository`/`IGenreRepository` directly.

## Consequences
- `MovieLogger.Api` never needs to reference repository or DAL types — its dependency surface stays limited to the Service layer's service interfaces.
- `MovieLogger.DAL` depends on `MovieLogger.Service` (to implement its interfaces) rather than the other way around, keeping the dependency direction pointing from persistence toward abstractions owned by business logic, not the reverse.
- If a future consumer (e.g. a background job or a second API) needed direct repository access bypassing the Service layer, it would need to reference `MovieLogger.Service` for the interfaces — this is an accepted constraint, not a gap, since bypassing services was never intended.
- Related: ADR-002 (Service layer), ADR-005/006/007 (repository pattern and its structure).
