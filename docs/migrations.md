# Database migrations (Flyway)

Schema creation, schema changes, and migration history tracking are managed by [Flyway](https://flywaydb.org/), not EF Core. See [ADR-012](adr/ADR-012-use-flyway-for-schema-migrations.md) for why.

EF Core (`MovieLogger.DAL`) still owns the `DbContext`, entity mapping, and all querying/persisting — it just no longer creates or changes the schema.

Migration files live in [`database/migrations/`](../database/migrations), named `V{n}__description.sql` (Flyway's default versioned-migration convention). Configuration for running them locally lives in [`database/flyway.conf`](../database/flyway.conf). Provisioning the database itself (outside Flyway's scope) is handled by [`database/setup/`](../database/setup).

## Installing Flyway locally

Flyway is a standalone Java-based CLI, not a .NET tool — install it separately:

- **Chocolatey**: `choco install flyway.commandline`
- **Scoop**: `scoop install flyway`
- **Manual**: download the Windows zip from the [Flyway releases page](https://documentation.red-gate.com/flyway/reference/usage/command-line) and add its directory to `PATH`.

Flyway needs a JVM available (bundled with recent Flyway CLI distributions; otherwise install a JDK/JRE separately).

## Local connection setup (Windows Integrated Auth)

The local SQL Server instance uses Windows Integrated Authentication, the same as EF Core's runtime connection string (`MovieLogger.Api/appsettings.json`). `database/flyway.conf` is configured accordingly:

```
flyway.url=jdbc:sqlserver://localhost;databaseName=MovieLoggerDb;integratedSecurity=true;trustServerCertificate=true;
```

No username or password is configured — integrated auth carries no credential to store, so there's nothing to keep out of source control here.

**Caveat:** Flyway connects over JDBC, and the Microsoft JDBC driver's `integratedSecurity=true` only works out of the box on a domain-joined machine (via Kerberos). On a standalone local machine — the expected setup for this project's local SQL Server instance — it instead needs the native `mssql-jdbc_auth-<version>-x64.dll` available on the system `PATH`. That DLL ships inside the Microsoft JDBC driver's distribution zip (in an `auth\x64` subfolder), not inside Flyway itself, so it has to be downloaded and placed separately:

1. Download the `mssql-jdbc` driver zip matching the version Flyway bundles (check with `flyway -v` or the Flyway SQL Server support docs) from the [Microsoft JDBC Driver for SQL Server releases](https://github.com/microsoft/mssql-jdbc/releases).
2. Copy `mssql-jdbc_auth-<version>-x64.dll` from the zip's `auth\x64` folder to a directory already on `PATH` (or add its folder to `PATH`).
3. Run `flyway -configFiles=database/flyway.conf info` to confirm Flyway can connect.

**Fallback:** if integrated auth via the DLL proves troublesome, create a dedicated SQL Server login for Flyway only (the app itself keeps using Windows auth) and switch `flyway.conf` to `flyway.user`/`flyway.password`, sourced from an environment variable or an untracked local override file — never committed. `flyway.conf` supports `flyway.password=${FLYWAY_PASSWORD}`-style environment variable placeholders for exactly this case.

## Creating the database (one-time, or after a local reset)

Flyway connects directly to `MovieLoggerDb` via the JDBC URL in `flyway.conf` — the database itself must already exist before `flyway migrate` can open a connection. Flyway does not create the database; it only manages the schema (tables, indexes, and its own migration history table) inside a database that's already there. This is standard Flyway behavior on SQL Server, not something specific to this project's setup.

If `MovieLoggerDb` doesn't exist yet — first-time local setup, or after dropping it locally — create it first:

```
.\database\setup\create-database.ps1
```

This runs an idempotent `CREATE DATABASE` against the local instance using the same Windows Integrated Authentication as Flyway and EF Core. It's safe to re-run even if the database already exists.

## Running migrations

From the repository root, pointing Flyway at the checked-in config:

```
flyway -configFiles=database/flyway.conf migrate
```

Other useful commands:

- `flyway -configFiles=database/flyway.conf info` — show applied/pending migrations and their status.
- `flyway -configFiles=database/flyway.conf validate` — verify applied migrations match the files on disk.

## Adding a new migration

1. Add a new file to `database/migrations/` named `V{next}__description.sql`, where `{next}` is one greater than the highest existing version.
2. Write forward-only T-SQL — there is no corresponding "down" migration; a schema change is reverted, if ever needed, by writing another forward migration.
3. If the change affects columns/relationships EF Core queries against, update the matching `IEntityTypeConfiguration<T>` in `MovieLogger.DAL/Configurations/` (and the entity in `MovieLogger.Service/Entities/`) to match — Flyway and EF's model are no longer kept in sync automatically.
4. Seed/reference data changes go in their own migration file, separate from schema changes, and should be written idempotently (e.g. `WHERE NOT EXISTS`) so they're safe if ever re-run outside Flyway's normal history-tracked `migrate` flow.
