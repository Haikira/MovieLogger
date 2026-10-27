#!/usr/bin/env bash
# Entry point of the migration image (database/Dockerfile). Creates the database and the API's
# least-privilege login if needed, then applies any pending Flyway migrations. Never drops or cleans.
# See docs/migrations.md ("Migrating a deployed database").
#
# Required environment variables:
#   DB_HOST             database server hostname (the RDS endpoint; certificates are checked against it)
#   DB_ADMIN_USER       admin login used for the bootstrap and Flyway (the RDS master user)
#   DB_ADMIN_PASSWORD   its password
#   APP_DB_PASSWORD     password for the API's login
# Optional:
#   DB_PORT (1433), DB_NAME (MovieLoggerDb), APP_DB_LOGIN (movielogger_app)
#
# Connections always use TLS with full certificate validation; there is deliberately no way to turn it off.
set -euo pipefail

fail() { echo "migrate: $*" >&2; exit 2; }

for name in DB_HOST DB_ADMIN_USER DB_ADMIN_PASSWORD APP_DB_PASSWORD; do
    [[ -n "${!name:-}" ]] || fail "$name is required."
done

DB_PORT="${DB_PORT:-1433}"
DB_NAME="${DB_NAME:-MovieLoggerDb}"
APP_DB_LOGIN="${APP_DB_LOGIN:-movielogger_app}"

# These values are substituted into SQL by sqlcmd, so only allow characters that can't break out of an
# identifier or string literal.
[[ "$DB_PORT" =~ ^[0-9]{1,5}$ ]] || fail "DB_PORT must be a port number."
[[ "$DB_HOST" =~ ^[A-Za-z0-9.-]+$ ]] || fail "DB_HOST must be a hostname."
[[ "$DB_NAME" =~ ^[A-Za-z][A-Za-z0-9_]{0,63}$ ]] || fail "DB_NAME must be letters, digits and underscores."
[[ "$APP_DB_LOGIN" =~ ^[A-Za-z][A-Za-z0-9_]{0,63}$ ]] || fail "APP_DB_LOGIN must be letters, digits and underscores."
[[ "$APP_DB_PASSWORD" =~ ^[A-Za-z0-9._~+=-]{16,128}$ ]] ||
    fail "APP_DB_PASSWORD must be 16-128 characters of letters, digits and . _ ~ + = - (no quotes, brackets or \$)."

echo "migrate: bootstrapping database ${DB_NAME} and login ${APP_DB_LOGIN} on ${DB_HOST}:${DB_PORT}"
# sqlcmd reads the admin password from SQLCMDPASSWORD and scripting variables from the environment, so no
# secret appears on a command line. -N mandatory: encrypt and validate the server certificate (trusted CAs
# in SSL_CERT_FILE, hostname must match DB_HOST). -b: stop with a non-zero exit code on the first error.
SQLCMDPASSWORD="$DB_ADMIN_PASSWORD" DatabaseName="$DB_NAME" AppLogin="$APP_DB_LOGIN" AppPassword="$APP_DB_PASSWORD" \
    sqlcmd -S "tcp:${DB_HOST},${DB_PORT}" -U "$DB_ADMIN_USER" -d master -N mandatory -b -i /migrator/bootstrap.sql

echo "migrate: applying Flyway migrations to ${DB_NAME}"
# encrypt=true;trustServerCertificate=false: the JDBC driver validates the certificate chain against the
# JRE trust store (which includes the RDS root CAs) and the hostname against DB_HOST.
export FLYWAY_URL="jdbc:sqlserver://${DB_HOST}:${DB_PORT};databaseName=${DB_NAME};encrypt=true;trustServerCertificate=false"
export FLYWAY_USER="$DB_ADMIN_USER"
export FLYWAY_PASSWORD="$DB_ADMIN_PASSWORD"
exec flyway -locations=filesystem:/flyway/sql -cleanDisabled=true -validateMigrationNaming=true -connectRetries=3 migrate
