-- Adds authentication support, aligns the catalogue/logging schema with the domain language used by
-- the frontend design (DisplayName, ReleaseYear, Synopsis, DateWatched/Rating/Notes), and adds a
-- dedicated Watchlist, distinct from the existing named Lists feature.
--
-- This project is still early/WIP (see README), so this migration favours forward-only clarity over
-- byte-perfect preservation of pre-existing local dev data (e.g. the 0-10 Score scale narrowing to a
-- 1-5 Rating is not remapped row-by-row).
--
-- GO batch separators are required wherever a later statement references a column that an earlier
-- statement in this file just added or renamed: SQL Server resolves column names for an entire batch
-- before executing any statement in it, so a same-batch reference to a brand-new/renamed column fails
-- with "Invalid column name" even though the column will exist by the time that statement would run.

-- ===== Users: rename Username -> DisplayName, add PasswordHash and UpdatedAt =====

EXEC sp_rename 'Users.Username', 'DisplayName', 'COLUMN';
GO

DROP INDEX [IX_Users_Username] ON [Users];

ALTER TABLE [Users] ADD [PasswordHash] nvarchar(256) NOT NULL CONSTRAINT [DF_Users_PasswordHash] DEFAULT '';
GO

ALTER TABLE [Users] DROP CONSTRAINT [DF_Users_PasswordHash];

ALTER TABLE [Users] ADD [UpdatedAt] datetime2 NULL;
GO

-- ===== Movies: ReleaseDate -> ReleaseYear, Description -> Synopsis, new catalogue fields =====

ALTER TABLE [Movies] ADD [ReleaseYear] int NULL;
GO

UPDATE [Movies] SET [ReleaseYear] = YEAR([ReleaseDate]);

ALTER TABLE [Movies] ALTER COLUMN [ReleaseYear] int NOT NULL;
GO

ALTER TABLE [Movies] DROP COLUMN [ReleaseDate];

EXEC sp_rename 'Movies.Description', 'Synopsis', 'COLUMN';
GO

ALTER TABLE [Movies] ADD [RuntimeMinutes] int NULL;
ALTER TABLE [Movies] ADD [PosterImageUrl] nvarchar(2000) NULL;

ALTER TABLE [Movies] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_Movies_CreatedAt] DEFAULT SYSUTCDATETIME();
GO

ALTER TABLE [Movies] DROP CONSTRAINT [DF_Movies_CreatedAt];

ALTER TABLE [Movies] ADD [CreatedByUserId] int NULL;
GO

ALTER TABLE [Movies] ADD CONSTRAINT [FK_Movies_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL;

CREATE INDEX [IX_Movies_CreatedByUserId] ON [Movies] ([CreatedByUserId]);
CREATE INDEX [IX_Movies_Title] ON [Movies] ([Title]);

-- ===== MovieWatches ("movie logs"): rename to DateWatched/Rating/Notes, tighten Rating to 1-5 =====

EXEC sp_rename 'MovieWatches.WatchedAt', 'DateWatched', 'COLUMN';
EXEC sp_rename 'MovieWatches.Score', 'Rating', 'COLUMN';
EXEC sp_rename 'MovieWatches.Review', 'Notes', 'COLUMN';
GO

ALTER TABLE [MovieWatches] ALTER COLUMN [Rating] int NULL;
ALTER TABLE [MovieWatches] ALTER COLUMN [Notes] nvarchar(500) NULL;
GO

ALTER TABLE [MovieWatches] ADD CONSTRAINT [CK_MovieWatches_Rating] CHECK ([Rating] IS NULL OR [Rating] BETWEEN 1 AND 5);

ALTER TABLE [MovieWatches] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_MovieWatches_CreatedAt] DEFAULT SYSUTCDATETIME();
GO

ALTER TABLE [MovieWatches] DROP CONSTRAINT [DF_MovieWatches_CreatedAt];

ALTER TABLE [MovieWatches] ADD [UpdatedAt] datetime2 NULL;

CREATE INDEX [IX_MovieWatches_DateWatched] ON [MovieWatches] ([DateWatched]);

-- ===== Watchlist: movies a user intends to watch (distinct from the existing named Lists feature) =====

CREATE TABLE [WatchlistItems] (
    [Id] int NOT NULL IDENTITY(1,1),
    [UserId] int NOT NULL,
    [MovieId] int NOT NULL,
    [DateAdded] datetime2 NOT NULL,
    CONSTRAINT [PK_WatchlistItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WatchlistItems_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_WatchlistItems_Movies_MovieId] FOREIGN KEY ([MovieId]) REFERENCES [Movies] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_WatchlistItems_UserId_MovieId] ON [WatchlistItems] ([UserId], [MovieId]);
CREATE INDEX [IX_WatchlistItems_MovieId] ON [WatchlistItems] ([MovieId]);
