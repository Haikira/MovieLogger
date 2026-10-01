# Development

Day-to-day tasks are run through the [`Makefile`](../Makefile) in the repository root. It orchestrates the tools the project already uses (`dotnet`, `npm`, Flyway); it doesn't replace them, and every target prints the underlying command it runs.

Run everything from the repository root.

## Prerequisites

| Tool | Used for | Notes |
| --- | --- | --- |
| [.NET 10 SDK](https://dotnet.microsoft.com/download) | Building, running and testing the API | |
| [Node.js](https://nodejs.org/) 20+ (includes npm) | The React frontend | Developed on Node 24 / npm 11 |
| GNU Make | Running the commands below | See [Installing GNU Make on Windows](#installing-gnu-make-on-windows) |
| SQL Server | The app database (`MovieLoggerDb`) and the API integration tests | Local default instance, Windows Integrated Authentication. See [`testing.md`](testing.md) |
| [Flyway CLI](https://documentation.red-gate.com/flyway) | `make database-migrate` / `make database-status` only | See [`migrations.md`](migrations.md) for installation and the JDBC auth DLL |

The API also needs a `Jwt:Key` user secret to start. If it's missing, `make start-api` stops with a message giving the exact `dotnet user-secrets set` command. The tests don't need it.

## Common commands

Run `make help` (or just `make`) to see this list in the terminal.

| Command | What it does |
| --- | --- |
| `make help` | Lists every target with a short description. |
| `make install-dependencies` | Installs the frontend packages exactly as locked in `client/package-lock.json` (`npm ci`). Run this first, and again after the lockfile changes. |
| `make database-migrate` | Applies any pending Flyway migrations to the local `MovieLoggerDb` (`flyway migrate`). It only applies new migrations: it never drops, cleans or baselines. |
| `make database-status` | Shows which migrations are applied or pending (`flyway info`). Read-only. |
| `make start-api` | Starts the API on http://localhost:5021 using the `http` launch profile (Development environment). |
| `make start-frontend` | Starts the Vite dev server on http://localhost:5173. It proxies `/api` to the API, so the API needs to be running too. |
| `make start-dev` | Starts the API and the frontend together in one terminal. Press Ctrl+C once to stop both. |
| `make build-api` | Builds the .NET solution (`MovieLogger.slnx`). |
| `make build-frontend` | Type-checks and builds the production frontend into `client/dist`. |
| `make test-api` | Runs all .NET tests: the service unit tests and the API integration tests. The integration tests need SQL Server (see [`testing.md`](testing.md)). |
| `make test-frontend` | Runs the frontend Vitest suite. Needs no API or database. |
| `make test-all` | Runs `test-api` then `test-frontend`, and fails if either fails. |
| `make verify-project` | The full check to run before committing: backend build, backend tests, frontend tests, frontend production build. Each stage is announced; the first failure stops the run and is reported as `verify-project FAILED at stage N of 4: <stage>`. |
| `make clean` | Removes build output: .NET build output via `dotnet clean`, and `client/dist`. It doesn't touch `node_modules`, source, migrations, configuration or the database. |

### First-time setup

```
make install-dependencies
make database-migrate
make start-dev
```

`database-migrate` needs `MovieLoggerDb` to exist. Flyway doesn't create databases, so on a brand-new machine run [`database/setup/create-database.ps1`](../database/setup/create-database.ps1) once first (see [`migrations.md`](migrations.md#creating-the-database-one-time-or-after-a-local-reset)).

### Notes on the run targets

- `make start-dev` runs the API and Vite as two parallel jobs of one `make` process in the current terminal, so their output is interleaved. There are no background processes: Ctrl+C reaches the API, Vite and `make` together and all of them stop.
- Vite's keyboard shortcuts (`h`, `r`, `o`, `q`) are unavailable under `make start-frontend` and `make start-dev`, because Vite's input comes from the null device. On Windows `npm` is a batch file, and Ctrl+C on a batch file normally leaves the terminal at a `Terminate batch job (Y/N)?` prompt; reading from the null device avoids that. Hot reload works as normal, and you can open http://localhost:5173 yourself. If you want the shortcuts, run `npm.cmd run dev` from `client/` directly.

## Installing GNU Make on Windows

Windows doesn't include `make`. The simplest option is the native Windows build of GNU Make 4.4.1 published by ezwinports, available through winget, which is built into Windows 11:

```
winget install --id ezwinports.make -e
```

This is a portable install: winget adds `make` to your user `PATH`. Open a new terminal afterwards, then check it:

```
make --version
```

It works from PowerShell, Command Prompt and Git Bash. You don't need WSL, MSYS2 or Chocolatey, and you don't need to change PowerShell's execution policy.

### How the Makefile handles Windows

- **Shell.** GNU Make on Windows uses `sh.exe` if it finds one on `PATH` (for example inside Git Bash) and `cmd.exe` otherwise. The Makefile pins `cmd.exe` on Windows so targets behave the same whichever terminal you use. Recipes avoid shell-specific syntax, so the same Makefile also works on macOS/Linux.
- **npm.** Recipes call `npm.cmd` explicitly. In PowerShell, plain `npm` resolves to `npm.ps1`, which a restrictive execution policy blocks; `npm.cmd` isn't affected.
- **No Unix file commands.** `make clean` uses `dotnet clean` and Node's `fs.rmSync` rather than `rm`/`rmdir`, so it doesn't depend on which shell is available.
