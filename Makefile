# MovieLogger developer tasks.
#
# Run `make help` (or just `make`) to list the targets. Requires GNU Make; on Windows install it
# with `winget install --id ezwinports.make -e`. See docs/development.md for setup details.

# -----------------------------------------------------------------------------
# Platform
# -----------------------------------------------------------------------------
# On Windows, GNU Make uses sh.exe if it happens to find one on PATH (e.g. when run from Git Bash)
# and cmd.exe otherwise. Pinning cmd.exe makes `make` behave the same from PowerShell, cmd or
# Git Bash. The recipes below avoid shell-specific syntax so they also run under the POSIX sh
# used on macOS/Linux; the variables set here cover the few differences.
ifeq ($(OS),Windows_NT)
  SHELL := cmd.exe
  # Call npm.cmd explicitly so npm never resolves to npm.ps1, which PowerShell's execution
  # policy can block.
  NPM := npm.cmd
  BLANK_LINE := echo.
  NULL_DEVICE := NUL
else
  NPM := npm
  BLANK_LINE := echo
  NULL_DEVICE := /dev/null
endif

# -----------------------------------------------------------------------------
# Paths and tools
# -----------------------------------------------------------------------------
SOLUTION      := MovieLogger.slnx
API_PROJECT   := server/src/MovieLogger.Api
API_PROFILE   := http
CLIENT_DIR    := client
FLYWAY_CONFIG := database/flyway.conf
API_IMAGE     := movielogger-api

DOTNET := dotnet
FLYWAY := flyway
NODE   := node
DOCKER := docker

# Extra arguments for the Vite dev server. start-dev sets --clearScreen false so Vite doesn't
# wipe the API's startup output from the shared terminal.
VITE_DEV_ARGS :=

.DEFAULT_GOAL := help

.PHONY: help install-dependencies start-api start-frontend start-dev build-api build-frontend \
        test-api test-frontend test-all verify-project database-status database-migrate clean \
        check-frontend-dependencies docker-build docker-run

# -----------------------------------------------------------------------------
# Help
# -----------------------------------------------------------------------------
define HELP_TEXT
MovieLogger development commands

Usage: make <target>

Setup
  install-dependencies  Install the frontend npm packages exactly as locked in
                        client/package-lock.json (npm ci).
  database-migrate      Apply any pending Flyway migrations to the local MovieLoggerDb
                        database. Never drops or cleans anything.
  database-status       Show which Flyway migrations are applied or pending (read-only).

Run (press Ctrl+C to stop)
  start-api             Start the ASP.NET Core API on http://localhost:5021.
  start-frontend        Start the Vite dev server on http://localhost:5173. It proxies
                        /api to the API, so start the API too (or use start-dev).
  start-dev             Start the API and the frontend together in this terminal.

Build
  build-api             Build the .NET solution (MovieLogger.slnx).
  build-frontend        Type-check and build the production frontend into client/dist.

Test
  test-api              Run all .NET tests: service unit tests and API integration
                        tests. The integration tests need the local SQL Server.
  test-frontend         Run the frontend Vitest suite (no API or database needed).
  test-all              Run test-api, then test-frontend. Fails if either fails.
  verify-project        Full check before committing: backend build, backend tests,
                        frontend tests, frontend production build. Stops at, and names,
                        the first stage that fails.

Docker (needs Docker installed; see docs/deployment.md)
  docker-build          Build the production API image (movielogger-api) from
                        server/Dockerfile.
  docker-run            Run the API image on http://localhost:8080 in the Production
                        environment. Passes ConnectionStrings__MovieLoggerDb, Jwt__Key
                        and Cors__AllowedOrigins__0 through from your environment.

Housekeeping
  clean                 Remove build output: .NET bin/obj output (dotnet clean) and
                        client/dist. Leaves node_modules, source and config alone.

First time? Run: make install-dependencies, then make database-migrate, then make start-dev.
See docs/development.md for prerequisites.
endef

help:
	$(info $(HELP_TEXT))
	@$(BLANK_LINE)

# -----------------------------------------------------------------------------
# Setup
# -----------------------------------------------------------------------------
install-dependencies:
	$(NPM) ci --prefix $(CLIENT_DIR)

# Frontend targets depend on this so a missing node_modules gives a clear message instead of a
# "vite is not recognized" error.
check-frontend-dependencies:
ifeq ($(wildcard $(CLIENT_DIR)/node_modules),)
	$(error Frontend dependencies are not installed. Run "make install-dependencies" first)
endif

# -----------------------------------------------------------------------------
# Run
# -----------------------------------------------------------------------------
# Uses the API's existing "http" launch profile (http://localhost:5021, Development environment).
start-api:
	$(DOTNET) run --project $(API_PROJECT) --launch-profile $(API_PROFILE)

# Input comes from the null device so Ctrl+C stops cleanly on Windows. npm is a batch file there
# (npm.cmd), and when Ctrl+C interrupts a batch file cmd.exe asks "Terminate batch job (Y/N)?"
# and waits for a key, leaving the terminal stuck after Vite has already stopped. With no keyboard
# input to wait for, it ends straight away. The trade-off is that Vite's keyboard shortcuts
# (h, r, o, q) are unavailable; the dev server and hot reload work as normal.
start-frontend: check-frontend-dependencies
	$(NPM) --prefix $(CLIENT_DIR) run dev -- $(VITE_DEV_ARGS) < $(NULL_DEVICE)

# Runs start-api and start-frontend as two parallel jobs of a single make process. Both share this
# console, so Ctrl+C reaches the API, Vite and make together and everything stops; no background
# processes are left behind.
start-dev: check-frontend-dependencies
	@echo Starting the API on http://localhost:5021 and the frontend on http://localhost:5173
	@echo Press Ctrl+C to stop both.
	@$(MAKE) --no-print-directory -j 2 start-api start-frontend "VITE_DEV_ARGS=--clearScreen false"

# -----------------------------------------------------------------------------
# Build
# -----------------------------------------------------------------------------
build-api:
	$(DOTNET) build $(SOLUTION)

build-frontend: check-frontend-dependencies
	$(NPM) --prefix $(CLIENT_DIR) run build

# -----------------------------------------------------------------------------
# Test
# -----------------------------------------------------------------------------
# Runs every test project in the solution, including the SQL Server-backed API integration tests.
test-api:
	$(DOTNET) test $(SOLUTION)

test-frontend: check-frontend-dependencies
	$(NPM) --prefix $(CLIENT_DIR) test

test-all: test-api test-frontend

# Each stage runs as its own make invocation so a failure can be reported by name.
# $(call run_stage,<stage number>,<description>,<target>)
define run_stage
	@$(BLANK_LINE)
	@echo ==== Stage $(1) of 4: $(2) ====
	@$(MAKE) --no-print-directory $(3) || ($(BLANK_LINE) && echo ==== verify-project FAILED at stage $(1) of 4: $(2) ==== && exit 1)
endef

verify-project:
	$(call run_stage,1,Backend build,build-api)
	$(call run_stage,2,Backend tests,test-api)
	$(call run_stage,3,Frontend tests,test-frontend)
	$(call run_stage,4,Frontend production build,build-frontend)
	@$(BLANK_LINE)
	@echo ==== verify-project PASSED: all 4 stages succeeded ====

# -----------------------------------------------------------------------------
# Database (Flyway is the schema authority; see docs/migrations.md)
# -----------------------------------------------------------------------------
database-status:
	$(FLYWAY) -configFiles=$(FLYWAY_CONFIG) info

# `flyway migrate` only applies pending versioned migrations; it never drops or cleans.
database-migrate:
	$(FLYWAY) -configFiles=$(FLYWAY_CONFIG) migrate

# -----------------------------------------------------------------------------
# Docker (see docs/deployment.md)
# -----------------------------------------------------------------------------
docker-build:
	$(DOCKER) build -t $(API_IMAGE) -f server/Dockerfile server

# `-e NAME` with no value copies NAME from the calling environment (and skips it if unset), so no
# configuration value or secret ever appears in this file or on the command line.
docker-run:
	$(DOCKER) run --rm -p 8080:8080 -e ConnectionStrings__MovieLoggerDb -e Jwt__Key -e Cors__AllowedOrigins__0 $(API_IMAGE)

# -----------------------------------------------------------------------------
# Housekeeping
# -----------------------------------------------------------------------------
# dotnet clean removes the solution's build output; Node (already required for the frontend)
# removes client/dist without relying on platform-specific rm/rmdir.
clean:
	$(DOTNET) clean $(SOLUTION) --nologo --verbosity minimal
	$(NODE) -e "require('fs').rmSync('$(CLIENT_DIR)/dist', { recursive: true, force: true }); console.log('Removed $(CLIENT_DIR)/dist')"
