namespace MovieLogger.Service.Entities
{
    public class User
    {
        public int Id { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public ICollection<UserMovie> UserMovies { get; set; } = new List<UserMovie>();

        public ICollection<MovieWatch> MovieWatches { get; set; } = new List<MovieWatch>();

        public ICollection<MovieList> Lists { get; set; } = new List<MovieList>();

        public ICollection<WatchlistItem> WatchlistItems { get; set; } = new List<WatchlistItem>();
    }
}
