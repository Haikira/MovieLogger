namespace MovieLogger.Service.Dtos.MovieWatches
{
    public class MovieWatchResponseDto
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int MovieId { get; set; }

        public DateTime DateWatched { get; set; }

        public int? Rating { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
