namespace MovieLogger.Service.Entities
{
    public class MovieList
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public User User { get; set; } = null!;

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; }

        public ICollection<ListMovie> ListMovies { get; set; } = new List<ListMovie>();
    }
}
