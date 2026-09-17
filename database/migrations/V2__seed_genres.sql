-- Seeds the standard set of movie genres with explicit, stable IDs.
-- Idempotent: safe to run again against a database that already has some or all of these rows
-- (e.g. a manual re-run outside Flyway's normal history tracking) without violating the primary
-- key or the unique index on Genres.Name, and without inserting duplicates.

SET IDENTITY_INSERT [Genres] ON;

INSERT INTO [Genres] ([Id], [Name])
SELECT [Id], [Name]
FROM (VALUES
    (1, 'Action'),
    (2, 'Adventure'),
    (3, 'Animation'),
    (4, 'Comedy'),
    (5, 'Crime'),
    (6, 'Documentary'),
    (7, 'Drama'),
    (8, 'Family'),
    (9, 'Fantasy'),
    (10, 'Horror'),
    (11, 'History'),
    (12, 'Music'),
    (13, 'Mystery'),
    (14, 'Romance'),
    (15, 'Science Fiction'),
    (16, 'Thriller'),
    (17, 'War'),
    (18, 'Western'),
    (19, 'Sport'),
    (20, 'Musical')
) AS [StandardGenres] ([Id], [Name])
WHERE NOT EXISTS (
    SELECT 1 FROM [Genres] [g] WHERE [g].[Id] = [StandardGenres].[Id]
);

SET IDENTITY_INSERT [Genres] OFF;
