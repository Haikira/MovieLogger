namespace MovieLogger.Service.Entities
{
    public class Movie
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public DateOnly ReleaseDate { get; set; }

        public string? Director { get; set; }

        public string? Description { get; set; }

        public ICollection<Genre> Genres { get; set; } = new List<Genre>();

        public ICollection<UserMovie> UserMovies { get; set; } = new List<UserMovie>();

        public ICollection<MovieWatch> MovieWatches { get; set; } = new List<MovieWatch>();

        public ICollection<ListMovie> ListMovies { get; set; } = new List<ListMovie>();
    }
}
