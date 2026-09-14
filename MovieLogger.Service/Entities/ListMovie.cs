namespace MovieLogger.Service.Entities
{
    public class ListMovie
    {
        public int ListId { get; set; }

        public MovieList List { get; set; } = null!;

        public int MovieId { get; set; }

        public Movie Movie { get; set; } = null!;

        public DateTime AddedAt { get; set; }
    }
}
