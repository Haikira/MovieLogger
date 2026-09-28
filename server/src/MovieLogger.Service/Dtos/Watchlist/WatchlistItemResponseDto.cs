namespace MovieLogger.Service.Dtos.Watchlist
{
    public class WatchlistItemResponseDto
    {
        public int Id { get; set; }

        public int MovieId { get; set; }

        public string Title { get; set; } = string.Empty;

        public int ReleaseYear { get; set; }

        public string? PosterImageUrl { get; set; }

        public DateTime DateAdded { get; set; }
    }
}
