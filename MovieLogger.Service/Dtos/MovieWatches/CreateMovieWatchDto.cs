using System.ComponentModel.DataAnnotations;

namespace MovieLogger.Service.Dtos.MovieWatches
{
    public class CreateMovieWatchDto
    {
        public int UserId { get; set; }

        public int MovieId { get; set; }

        public DateTime WatchedAt { get; set; }

        [Range(typeof(decimal), "0", "10")]
        public decimal? Score { get; set; }

        [MaxLength(4000)]
        public string? Review { get; set; }
    }
}
