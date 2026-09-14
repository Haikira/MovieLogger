# ADR-009: Use AutoMapper

## Status
Accepted

## Context
The Service layer translates between EF Core entities (e.g. `Movie`, `Genre`) and DTOs used at the API boundary. Writing and maintaining this mapping by hand for every entity/DTO pair is repetitive and error-prone as fields are added or renamed on either side.

## Decision
Use AutoMapper (`AutoMapper` v16.2.0, referenced in `MovieLogger.Service\MovieLogger.Service.csproj`) to handle entity-to-DTO mapping. Mapping rules are declared in profile classes — `MovieProfile.cs` and `GenreProfile.cs` under `MovieLogger.Service\Mapping\` — and registered via `services.AddAutoMapper(_ => { }, typeof(MovieProfile).Assembly)` in `MovieLogger.Service\Extensions\ServiceCollectionExtensions.cs`. Services such as `MovieService` and `GenreService` consume mapping through a constructor-injected `IMapper`.

## Consequences
- Mapping logic is centralized in profile classes rather than scattered across services as manual property assignment, reducing duplication and copy-paste errors.
- Adding a new entity/DTO pair mostly means adding a profile rather than hand-writing a mapping method.
- Mapping becomes partly configuration-driven (profiles) rather than plain code, which can make some field-level mapping bugs harder to spot at compile time and easier to catch only via tests — the Service layer's test project should cover mapping-dependent behavior.
