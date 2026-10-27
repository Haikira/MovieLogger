-- Creates the application database and the API's least-privilege login, if they don't exist yet.
-- Run by migrate.sh with sqlcmd against master, as the database admin (the RDS master user), before
-- Flyway applies the migrations. Safe to run on every deployment.
--
-- sqlcmd variables (validated by migrate.sh before they reach this script):
--   $(DatabaseName)  database to create, e.g. MovieLoggerDb
--   $(AppLogin)      SQL login the API connects as, e.g. movielogger_app
--   $(AppPassword)   that login's password (from the secrets store)

SET NOCOUNT ON;

IF DB_ID(N'$(DatabaseName)') IS NULL
BEGIN
    PRINT N'Creating database $(DatabaseName).';
    CREATE DATABASE [$(DatabaseName)];
END
ELSE
    PRINT N'Database $(DatabaseName) already exists.';
GO

-- The password is reset on every run so the login always matches the current secret.
IF SUSER_ID(N'$(AppLogin)') IS NULL
BEGIN
    PRINT N'Creating login $(AppLogin).';
    CREATE LOGIN [$(AppLogin)] WITH PASSWORD = N'$(AppPassword)', CHECK_POLICY = ON, DEFAULT_DATABASE = [$(DatabaseName)];
END
ELSE
BEGIN
    PRINT N'Login $(AppLogin) already exists; updating its password.';
    ALTER LOGIN [$(AppLogin)] WITH PASSWORD = N'$(AppPassword)';
END
GO

USE [$(DatabaseName)];
GO

-- The API only reads and writes rows. It can't change the schema (Flyway does that, as the admin)
-- and has no access to other databases.
IF DATABASE_PRINCIPAL_ID(N'$(AppLogin)') IS NULL
BEGIN
    PRINT N'Creating database user $(AppLogin).';
    CREATE USER [$(AppLogin)] FOR LOGIN [$(AppLogin)];
END

IF IS_ROLEMEMBER(N'db_datareader', N'$(AppLogin)') = 0
    ALTER ROLE db_datareader ADD MEMBER [$(AppLogin)];

IF IS_ROLEMEMBER(N'db_datawriter', N'$(AppLogin)') = 0
    ALTER ROLE db_datawriter ADD MEMBER [$(AppLogin)];

PRINT N'Login $(AppLogin) can read and write data in $(DatabaseName).';
GO
