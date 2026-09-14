using MovieLogger.Service.Enums;

namespace MovieLogger.Service.Entities
{
    public class Genre
    {
        public int Id { get; set; }

        public GenreTitle Title { get; set; }

        public ICollection<Movie> Movies { get; set; } = new List<Movie>();
    }
}
