using System.ComponentModel.DataAnnotations;

namespace MovieLogger.Service.Dtos.Movies
{
    public class MovieSearchQueryDto
    {
        [MaxLength(200)]
        public string? Title { get; set; }

        [MaxLength(200)]
        public string? Director { get; set; }

        [Range(1888, 2200)]
        public int? Year { get; set; }

        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 20;
    }
}
