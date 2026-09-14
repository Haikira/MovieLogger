# ADR-001: Use ASP.NET Core Web API

## Status
Accepted

## Context
MovieLogger needs an HTTP-facing entry point that clients (web, mobile, or tooling like Swagger UI) can call to log and query movies and genres. The team is already working in .NET, so the framework needed to be a first-class .NET web framework with strong tooling support, built-in dependency injection, and easy integration with EF Core and OpenAPI.

## Decision
Use ASP.NET Core Web API for the presentation/HTTP layer, implemented in the `MovieLogger.Api` project (`Microsoft.NET.Sdk.Web`). Controllers under `MovieLogger.Api\Controllers\` (e.g. `MoviesController.cs`) expose the API surface, with `Program.cs` wiring up services and middleware. Swagger/OpenAPI is enabled for interactive API documentation, as noted in the repo README.

## Consequences
- Gets built-in DI, model binding/validation, middleware pipeline, and Swagger generation for free, minimizing boilerplate.
- Keeps the API layer thin: controllers only depend on the Service layer (see ADR-002), not on EF Core or the DAL directly.
- Ties the project to the ASP.NET Core release/support cadence; upgrades need to track .NET's own lifecycle.
