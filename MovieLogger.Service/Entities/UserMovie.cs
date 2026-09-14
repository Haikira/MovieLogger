namespace MovieLogger.Service.Entities
{
    public class UserMovie
    {
        public int UserId { get; set; }

        public User User { get; set; } = null!;

        public int MovieId { get; set; }

        public Movie Movie { get; set; } = null!;

        public bool IsFavourite { get; set; }

        public bool IsOwned { get; set; }
    }
}
