namespace MovieLogger.Service.Dtos.MovieWatches
{
    public class MyMovieResponseDto
    {
        public int MovieId { get; set; }

        public string Title { get; set; } = string.Empty;

        public int ReleaseYear { get; set; }

        public string? Director { get; set; }

        public int? RuntimeMinutes { get; set; }

        public DateTime LastWatchedAt { get; set; }

        public int? LastRating { get; set; }

        public int TimesWatched { get; set; }
    }
}
