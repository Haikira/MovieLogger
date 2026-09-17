namespace MovieLogger.Service.Entities
{
    public class User
    {
        public int Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public ICollection<UserMovie> UserMovies { get; set; } = new List<UserMovie>();

        public ICollection<MovieWatch> MovieWatches { get; set; } = new List<MovieWatch>();

        public ICollection<MovieList> Lists { get; set; } = new List<MovieList>();
    }
}
