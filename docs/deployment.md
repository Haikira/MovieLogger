# Deployment

The API is packaged as a container image ([`server/Dockerfile`](../server/Dockerfile)) and configured entirely through environment variables. Nothing environment-specific or secret is committed. See [ADR-015](adr/ADR-015-package-the-api-as-a-container-configured-through-the-environment.md) for the reasoning.

The React frontend is built and hosted separately (see [`client/README.md`](../client/README.md)).

## Required configuration

ASP.NET Core maps environment variables onto configuration keys, with `__` (double underscore) standing for `:`. The API **refuses to start** if a required value is missing or invalid, and the error names the setting without printing its value.

| Environment variable | Configuration key | Required | Notes |
| --- | --- | --- | --- |
| `ASPNETCORE_ENVIRONMENT` | n/a | No | Defaults to `Production` in the container. Never use `Development` in a deployed environment: it enables Swagger and loads the local connection string. |
| `ConnectionStrings__MovieLoggerDb` | `ConnectionStrings:MovieLoggerDb` | **Yes** | SQL Server connection string. A secret when it contains a password: supply it from a secrets store, never from a committed file. Only `appsettings.Development.json` contains a (local, Windows-authenticated) connection string. |
| `Jwt__Key` | `Jwt:Key` | **Yes** | JWT signing key, **at least 32 bytes**. Secret. Generate a long random value, e.g. `openssl rand -base64 48`. Changing it signs every user out. |
| `Cors__AllowedOrigins__0`, `__1`, ... | `Cors:AllowedOrigins` | When the frontend is on another origin | Each value is one origin, `scheme://host[:port]` with no path or trailing slash, e.g. `https://app.example.com`. |

`Jwt:Issuer`, `Jwt:Audience` and `Jwt:ExpirationMinutes` have committed defaults in `appsettings.json` and can be overridden the same way (`Jwt__Issuer` etc.).

### CORS

The frontend and API are hosted on different origins, so the browser only lets the frontend call the API if the API allows that origin.

- Origins come only from `Cors:AllowedOrigins`. With none configured, CORS is off. That's the case in local development, where the Vite dev server proxies `/api` and everything is same-origin.
- The policy allows `GET`, `POST`, `PUT` and `DELETE` with the `Authorization` and `Content-Type` headers. It doesn't allow credentials (cookies): the API uses bearer tokens.
- A value that isn't a bare origin (e.g. one with a trailing slash) stops the API at startup, since it would never match a browser's `Origin` header.

## Health check

`GET /health` returns `200` with the body `Healthy` while the process is running. It needs no authentication and deliberately doesn't check the database, so a database outage doesn't cause the load balancer to replace otherwise healthy API containers. Use it as the load balancer / container health check path.

## Running behind a load balancer

The container listens on plain HTTP on port **8080**. TLS is expected to terminate at the load balancer, which forwards `X-Forwarded-For` and `X-Forwarded-Proto`. The API honours those headers (from any proxy, because a load balancer's address isn't fixed), so:

- the container must only accept traffic from the load balancer (restrict this with its security group), otherwise clients could spoof their IP or scheme;
- requests that reached the load balancer over HTTPS are treated as HTTPS.

Redirecting HTTP to HTTPS is best done by the load balancer's HTTP listener. The API's own HTTPS redirection only runs when it knows the public HTTPS port; set `ASPNETCORE_HTTPS_PORT=443` if you want it. Without it, the API logs `Failed to determine the https port for redirect` once and doesn't redirect, which is harmless.

## Building and running the container locally

Requires Docker. From the repository root:

```
make docker-build
```

which runs `docker build -t movielogger-api -f server/Dockerfile server`. The build context is `server/`, filtered by [`server/.dockerignore`](../server/.dockerignore), so test projects, build output, user files and local database files never enter the image.

To run it, set the configuration in your shell and run `make docker-run`, which passes those variables through to the container by name (`docker run --rm -p 8080:8080 -e ConnectionStrings__MovieLoggerDb -e Jwt__Key -e Cors__AllowedOrigins__0 movielogger-api`). In PowerShell:

```powershell
$env:ConnectionStrings__MovieLoggerDb = "<connection string>"
$env:Jwt__Key = "<at least 32 random bytes>"
make docker-run
```

Then `http://localhost:8080/health` should return `Healthy`.

The container runs on Linux, so it can't use the Windows Integrated Authentication in the local development connection string. To call endpoints that use the database from the container, connect with a SQL Server login (and `Server=host.docker.internal` to reach SQL Server on the host). `/health` works with any non-empty connection string because it never opens it.

## Database migrations

The API **doesn't** create or migrate the database, at startup or otherwise. Flyway is the schema authority ([ADR-012](adr/ADR-012-use-flyway-for-schema-migrations.md)): run `flyway migrate` against the target database as a separate deployment step, before starting a version of the API that needs the new schema. The database must already exist. [`database/flyway.conf`](../database/flyway.conf) is for the local instance only; for another database, pass the connection to the Flyway CLI with `-url`, `-user` and `-password` (or the `FLYWAY_URL`, `FLYWAY_USER` and `FLYWAY_PASSWORD` environment variables) rather than committing it. See [`migrations.md`](migrations.md).
