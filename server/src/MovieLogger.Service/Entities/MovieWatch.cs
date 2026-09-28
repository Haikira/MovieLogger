namespace MovieLogger.Service.Entities
{
    public class MovieWatch
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public User User { get; set; } = null!;

        public int MovieId { get; set; }

        public Movie Movie { get; set; } = null!;

        public DateTime DateWatched { get; set; }

        public int? Rating { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
