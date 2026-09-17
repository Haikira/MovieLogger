# Creates the local MovieLoggerDb database if it doesn't already exist.
#
# Flyway (and EF Core at runtime) connect directly to MovieLoggerDb - they don't create it.
# Run this once for first-time local setup, or again after dropping the database locally,
# before running `flyway migrate`. Idempotent: safe to re-run if the database already exists.
#
# Uses Windows Integrated Authentication against the local SQL Server instance, matching
# database/flyway.conf and MovieLogger.Api/appsettings.json.

$ErrorActionPreference = 'Stop'

$sql = @"
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'MovieLoggerDb')
BEGIN
    CREATE DATABASE [MovieLoggerDb];
END
"@

sqlcmd -S localhost -E -C -Q $sql
