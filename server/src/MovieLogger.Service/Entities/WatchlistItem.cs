namespace MovieLogger.Service.Entities
{
    public class WatchlistItem
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public User User { get; set; } = null!;

        public int MovieId { get; set; }

        public Movie Movie { get; set; } = null!;

        public DateTime DateAdded { get; set; }
    }
}
