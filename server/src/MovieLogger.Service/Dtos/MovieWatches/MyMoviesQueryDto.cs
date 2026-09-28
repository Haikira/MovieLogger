using System.ComponentModel.DataAnnotations;

namespace MovieLogger.Service.Dtos.MovieWatches
{
    public enum MyMoviesSort
    {
        DateWatchedDesc,
        DateWatchedAsc,
        TitleAsc,
        TitleDesc,
        RatingDesc,
        RatingAsc
    }

    public class MyMoviesQueryDto
    {
        [MaxLength(200)]
        public string? Search { get; set; }

        public int? GenreId { get; set; }

        [Range(1, 5)]
        public int? Rating { get; set; }

        public MyMoviesSort Sort { get; set; } = MyMoviesSort.DateWatchedDesc;

        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 20;
    }
}
