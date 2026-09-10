namespace MovieLogger.Service.Entities
{
    public class Movie
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public int ReleaseYear { get; set; }

        public string? Director { get; set; }

        public string? Description { get; set; }

        public ICollection<Genre> Genres { get; set; } = new List<Genre>();
    }
}
