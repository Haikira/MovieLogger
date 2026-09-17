namespace MovieLogger.Service.Dtos.MovieWatches
{
    public class MovieWatchResponseDto
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int MovieId { get; set; }

        public DateTime WatchedAt { get; set; }

        public decimal? Score { get; set; }

        public string? Review { get; set; }
    }
}
