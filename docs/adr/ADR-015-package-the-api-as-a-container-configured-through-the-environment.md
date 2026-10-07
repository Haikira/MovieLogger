# ADR-015: Package the API as a container configured through the environment

## Status
Accepted

## Context
MovieLogger is being prepared for AWS. The React frontend will be hosted on AWS Amplify and the API on ECS/Fargate behind a load balancer, so they'll be on different origins. Until now the API only ran locally: the base `appsettings.json` held the local Windows-authenticated SQL Server connection string (so a deployed instance missing its own connection string would quietly try `localhost`), the JWT key was only checked for presence, there was no health endpoint, no CORS policy, and nothing told the app that requests had arrived at a TLS-terminating proxy over HTTPS.

## Decision
- The API is built into a container image by a multi-stage `server/Dockerfile`: the .NET 10 SDK image publishes `MovieLogger.Api` (with `MovieLogger.Service` and `MovieLogger.DAL`) in Release, and the final image is the .NET 10 ASP.NET runtime image running as its non-root user and listening on HTTP port 8080. The build context is `server/`, with a `.dockerignore` excluding tests, build output, user files and local database files.
- All deployment-specific configuration comes from environment variables: `ConnectionStrings__MovieLoggerDb`, `Jwt__Key`, `Cors__AllowedOrigins__N` and `ASPNETCORE_ENVIRONMENT`. No secret or environment-specific value is committed. The local connection string moves from `appsettings.json` to `appsettings.Development.json`; local development keeps using `dotnet user-secrets` for `Jwt:Key`.
- The API fails at startup if the connection string is missing (checked in `AddMovieLoggerDal`), if `Jwt:Key` is missing or shorter than 32 bytes (the HMAC-SHA256 minimum), or if a configured CORS origin isn't a bare `scheme://host[:port]` origin. Error messages name the setting, never its value.
- CORS is driven by `Cors:AllowedOrigins`. With no origins it isn't enabled at all. Otherwise only those origins may call the API, with `GET`/`POST`/`PUT`/`DELETE` and the `Authorization`/`Content-Type` headers, and without credentials, since authentication is a bearer token rather than a cookie.
- Forwarded headers (`X-Forwarded-For`, `X-Forwarded-Proto`) are processed first in the pipeline, from any proxy, so HTTPS redirection and anything else reading the request scheme or client IP see the original values.
- `GET /health` is an anonymous liveness check that doesn't touch the database.
- The API still never migrates the database; Flyway runs as a separate step ([ADR-012](ADR-012-use-flyway-for-schema-migrations.md)).

## Consequences
- The same image runs in any environment; only its environment variables differ. Misconfiguration surfaces as a failed container start with a clear message instead of failures on the first request.
- Running the API outside Development without the required variables, including `dotnet run` with a non-Development profile, now fails at startup. `make start-api` (Development) is unaffected.
- Trusting forwarded headers from any proxy means the container must only be reachable through the load balancer. That has to be enforced in the network configuration (security groups), not in the app.
- The health check says nothing about the database. A readiness check that does could be added later if it's needed, without changing the liveness check.
- A Linux container can't use Windows Integrated Authentication, so deployed environments need a SQL Server login, whose password belongs in a secrets store and reaches the app through `ConnectionStrings__MovieLoggerDb`.
- Each new frontend origin has to be added to `Cors:AllowedOrigins` before the browser can call the API from it.
