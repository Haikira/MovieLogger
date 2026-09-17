-- Initial schema for MovieLoggerDb.
-- Mirrors the EF Core "InitialCreate" migration (MovieLogger.DAL/Migrations, now removed) as of the
-- SQL Server migration. Flyway is now the source of truth for schema creation and changes; EF Core
-- remains responsible only for the DbContext, entity mapping, querying and persistence.

CREATE TABLE [Genres] (
    [Id] int NOT NULL IDENTITY(1,1),
    [Name] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_Genres] PRIMARY KEY ([Id])
);

CREATE TABLE [Movies] (
    [Id] int NOT NULL IDENTITY(1,1),
    [Title] nvarchar(200) NOT NULL,
    [ReleaseDate] date NOT NULL,
    [Director] nvarchar(200) NULL,
    [Description] nvarchar(2000) NULL,
    CONSTRAINT [PK_Movies] PRIMARY KEY ([Id])
);

CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY(1,1),
    [Username] nvarchar(100) NOT NULL,
    [Email] nvarchar(256) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);

CREATE TABLE [MovieGenres] (
    [MovieId] int NOT NULL,
    [GenreId] int NOT NULL,
    CONSTRAINT [PK_MovieGenres] PRIMARY KEY ([MovieId], [GenreId]),
    CONSTRAINT [FK_MovieGenres_Genres_GenreId] FOREIGN KEY ([GenreId]) REFERENCES [Genres] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_MovieGenres_Movies_MovieId] FOREIGN KEY ([MovieId]) REFERENCES [Movies] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Lists] (
    [Id] int NOT NULL IDENTITY(1,1),
    [UserId] int NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(2000) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Lists] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Lists_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [MovieWatches] (
    [Id] int NOT NULL IDENTITY(1,1),
    [UserId] int NOT NULL,
    [MovieId] int NOT NULL,
    [WatchedAt] datetime2 NOT NULL,
    [Score] decimal(3,1) NULL,
    [Review] nvarchar(4000) NULL,
    CONSTRAINT [PK_MovieWatches] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MovieWatches_Movies_MovieId] FOREIGN KEY ([MovieId]) REFERENCES [Movies] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_MovieWatches_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [UserMovies] (
    [UserId] int NOT NULL,
    [MovieId] int NOT NULL,
    [IsFavourite] bit NOT NULL,
    [IsOwned] bit NOT NULL,
    CONSTRAINT [PK_UserMovies] PRIMARY KEY ([UserId], [MovieId]),
    CONSTRAINT [FK_UserMovies_Movies_MovieId] FOREIGN KEY ([MovieId]) REFERENCES [Movies] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserMovies_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ListMovies] (
    [ListId] int NOT NULL,
    [MovieId] int NOT NULL,
    [AddedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ListMovies] PRIMARY KEY ([ListId], [MovieId]),
    CONSTRAINT [FK_ListMovies_Lists_ListId] FOREIGN KEY ([ListId]) REFERENCES [Lists] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ListMovies_Movies_MovieId] FOREIGN KEY ([MovieId]) REFERENCES [Movies] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_Genres_Name] ON [Genres] ([Name]);

CREATE INDEX [IX_ListMovies_MovieId] ON [ListMovies] ([MovieId]);

CREATE INDEX [IX_Lists_UserId] ON [Lists] ([UserId]);

CREATE INDEX [IX_MovieGenres_GenreId] ON [MovieGenres] ([GenreId]);

CREATE INDEX [IX_MovieWatches_MovieId] ON [MovieWatches] ([MovieId]);

CREATE INDEX [IX_MovieWatches_UserId] ON [MovieWatches] ([UserId]);

CREATE INDEX [IX_UserMovies_MovieId] ON [UserMovies] ([MovieId]);

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);

CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);
