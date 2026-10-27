# Deployment

The API is packaged as a container image ([`server/Dockerfile`](../server/Dockerfile)) and configured entirely through environment variables. Nothing environment-specific or secret is committed. See [ADR-015](adr/ADR-015-package-the-api-as-a-container-configured-through-the-environment.md) for the reasoning.

The React frontend is built separately (see [`client/README.md`](../client/README.md)). On AWS, CloudFront serves the frontend from S3 and forwards `/api/*` to the API through an internal load balancer, so both share one origin. [ADR-016](adr/ADR-016-host-on-aws-with-cloudfront-ecs-fargate-and-rds.md) describes the architecture, its costs and the alternatives considered.

## Required configuration

ASP.NET Core maps environment variables onto configuration keys, with `__` (double underscore) standing for `:`. The API **refuses to start** if a required value is missing or invalid, and the error names the setting without printing its value.

| Environment variable | Configuration key | Required | Notes |
| --- | --- | --- | --- |
| `ASPNETCORE_ENVIRONMENT` | n/a | No | Defaults to `Production` in the container. Never use `Development` in a deployed environment: it enables Swagger and loads the local connection string. |
| `ConnectionStrings__MovieLoggerDb` | `ConnectionStrings:MovieLoggerDb` | **Yes** | SQL Server connection string. A secret when it contains a password: supply it from a secrets store, never from a committed file. Only `appsettings.Development.json` contains a (local, Windows-authenticated) connection string. |
| `Jwt__Key` | `Jwt:Key` | **Yes** | JWT signing key, **at least 32 bytes**. Secret. Generate a long random value, e.g. `openssl rand -base64 48`. Changing it signs every user out. |
| `Cors__AllowedOrigins__0`, `__1`, ... | `Cors:AllowedOrigins` | When the frontend is on another origin | Each value is one origin, `scheme://host[:port]` with no path or trailing slash, e.g. `https://app.example.com`. |

`Jwt:Issuer`, `Jwt:Audience` and `Jwt:ExpirationMinutes` have committed defaults in `appsettings.json` and can be overridden the same way (`Jwt__Issuer` etc.).

### Abuse protection

| Environment variable | Default | Notes |
| --- | --- | --- |
| `ForwardedHeaders__ForwardLimit` | `1` | Number of proxies in front of the API. Set it to **`2`** behind CloudFront and the load balancer, so the API sees the visitor's IP rather than CloudFront's. Must be at least 1. |
| `Registration__Enabled` | `true` | `false` closes sign-up (`POST /api/auth/register` returns 403 with a message). Existing users can still log in. |
| `RateLimiting__Enabled` | `true` | Per-client-IP limits. Rejected requests get `429` with a `Retry-After` header. `/health` is never limited. |
| `RateLimiting__RequestsPerMinute` | `300` | All requests from one client. |
| `RateLimiting__LoginAttemptsPerFiveMinutes` | `10` | `POST /api/auth/login`. |
| `RateLimiting__RegistrationsPerHour` | `5` | `POST /api/auth/register`. |

The limits are per API container and held in memory, so they reset when the container restarts. That's enough for a single-task demo; several containers would each apply their own limits.

Only the user who added a movie (`CreatedByUserId`) can edit (`PUT`) or delete it; anyone else gets 403. Deleting a movie cascades to every user's watch logs, watchlist entries and list items for it.

### CORS

CORS is only needed when the frontend and API are on different origins, because the browser then only lets the frontend call the API if the API allows that origin. The AWS deployment serves both from the same CloudFront hostname, so it leaves `Cors:AllowedOrigins` empty.

- Origins come only from `Cors:AllowedOrigins`. With none configured, CORS is off. That's the case in local development, where the Vite dev server proxies `/api` and everything is same-origin.
- The policy allows `GET`, `POST`, `PUT` and `DELETE` with the `Authorization` and `Content-Type` headers. It doesn't allow credentials (cookies): the API uses bearer tokens.
- A value that isn't a bare origin (e.g. one with a trailing slash) stops the API at startup, since it would never match a browser's `Origin` header.

## Health check

`GET /health` returns `200` with the body `Healthy` while the process is running. It needs no authentication and deliberately doesn't check the database, so a database outage doesn't cause the load balancer to replace otherwise healthy API containers. Use it as the load balancer / container health check path.

## Running behind a load balancer

The container listens on plain HTTP on port **8080**. TLS is expected to terminate at the load balancer, which forwards `X-Forwarded-For` and `X-Forwarded-Proto`. The API honours those headers (from any proxy, because a load balancer's address isn't fixed), so:

- the container must only accept traffic from the load balancer (restrict this with its security group), otherwise clients could spoof their IP or scheme;
- requests that reached the load balancer over HTTPS are treated as HTTPS.

Redirecting HTTP to HTTPS is best done by whatever terminates TLS. The API's own HTTPS redirection only runs when it knows the public HTTPS port (`ASPNETCORE_HTTPS_PORT`). Without it, the API logs `Failed to determine the https port for redirect` once and doesn't redirect, which is harmless.

### Behind CloudFront and an internal load balancer (AWS)

In the AWS deployment ([ADR-016](adr/ADR-016-host-on-aws-with-cloudfront-ecs-fargate-and-rds.md)) TLS terminates at **CloudFront**, which redirects HTTP viewers to HTTPS. CloudFront reaches an internal load balancer over plain HTTP, and the load balancer forwards to the container. As a result:

- set `ForwardedHeaders__ForwardLimit=2`, so the client IP is the address CloudFront recorded rather than CloudFront's own;
- the load balancer sets `X-Forwarded-Proto: http`, so the API sees requests as HTTP. **Leave `ASPNETCORE_HTTPS_PORT` unset**: otherwise the API would redirect every request to HTTPS, and CloudFront would forward it as HTTP again, in a loop;
- the network rules make trusting forwarded headers safe: the task's security group accepts port 8080 only from the load balancer's security group, and the load balancer only accepts traffic from CloudFront.

## Database connection and TLS (RDS)

The API connects to RDS for SQL Server with encryption **and** full certificate validation. The connection string has this shape (a secret: it lives in SSM Parameter Store, never in a committed file):

```
Server=<db-instance>.<id>.eu-north-1.rds.amazonaws.com,1433;Database=MovieLoggerDb;User Id=<app login>;Password=<password>;Encrypt=True
```

- **Don't add `TrustServerCertificate=True`.** It keeps the connection encrypted but stops the driver checking that it's talking to the real database, which defeats the point of TLS.
- **Use the RDS endpoint hostname**, not an IP address, because the certificate is issued for that name and the driver checks it.
- **The image trusts the Amazon RDS root CAs for `eu-north-1`.** [`server/Dockerfile`](../server/Dockerfile) adds the certificates in [`server/certs/aws-rds`](../server/certs/aws-rds/README.md) (from AWS's published regional bundle) to the container's system trust store with `update-ca-certificates`; .NET on Linux validates against that store. A database in another region needs that region's bundle.
- **The database enforces it too:** its parameter group sets `rds.force_ssl=1`, so unencrypted connections are refused.

How this was checked locally (no AWS connection):

1. Inside the built image, .NET builds a trusted chain for each of the three RDS root CAs, and rejects an unknown self-signed CA (`UntrustedRoot`).
2. The container, given `Encrypt=True` and pointed at a local SQL Server with a self-signed certificate, refuses the connection with `Certificate failed chain validation … UntrustedRoot` and `Certificate name mismatch`. Validation is enforced, not bypassed. The HTTP response is a bare `500` with no details, and the connection-string password appears nowhere in the logs.

A successful validated connection to RDS itself can only be confirmed once the database exists.

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

The image only trusts public CAs and the Amazon RDS root CAs. A local SQL Server normally has a self-signed certificate, so an encrypted connection from the container to it fails certificate validation, by design. For local container testing against such a server only, you can add `TrustServerCertificate=True` to the connection string you pass in, or use `Encrypt=False`. Never use either for RDS.

## Database migrations

The API **doesn't** create or migrate the database, at startup or otherwise. Flyway is the schema authority ([ADR-012](adr/ADR-012-use-flyway-for-schema-migrations.md)): run `flyway migrate` against the target database as a separate deployment step, before starting a version of the API that needs the new schema. The database must already exist. [`database/flyway.conf`](../database/flyway.conf) is for the local instance only; for another database, pass the connection to the Flyway CLI with `-url`, `-user` and `-password` (or the `FLYWAY_URL`, `FLYWAY_USER` and `FLYWAY_PASSWORD` environment variables) rather than committing it. See [`migrations.md`](migrations.md).

For a deployed database, use the migration image (`database/Dockerfile`, built with `make docker-build-migrator`). It creates the database and the API's least-privilege login if needed, then runs `flyway migrate`, always over validated TLS. Its configuration and checks are in [`migrations.md`](migrations.md#migrating-a-deployed-database-the-migration-image).
