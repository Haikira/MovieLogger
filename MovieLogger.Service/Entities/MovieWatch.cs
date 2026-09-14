namespace MovieLogger.Service.Entities
{
    public class MovieWatch
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public User User { get; set; } = null!;

        public int MovieId { get; set; }

        public Movie Movie { get; set; } = null!;

        public DateTime WatchedAt { get; set; }

        public decimal? Score { get; set; }

        public string? Review { get; set; }
    }
}
