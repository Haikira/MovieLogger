using System.ComponentModel.DataAnnotations;
using MovieLogger.Service.Dtos.Validation;

namespace MovieLogger.Service.Dtos.MovieWatches
{
    public class CreateMovieWatchDto
    {
        public int MovieId { get; set; }

        [NotInFuture]
        public DateTime DateWatched { get; set; }

        [Range(1, 5)]
        public int? Rating { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
