using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using MovieLogger.Service.Dtos.MovieWatches;

namespace MovieLogger.Service.Tests.Services
{
    public class MovieWatchDtoValidationTests
    {
        private static IList<ValidationResult> Validate(CreateMovieWatchDto dto)
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);
            return results;
        }

        [Fact]
        public void DateWatched_InThePast_IsValid()
        {
            var dto = new CreateMovieWatchDto { MovieId = 1, DateWatched = DateTime.UtcNow.AddDays(-1) };

            Validate(dto).Should().BeEmpty();
        }

        [Fact]
        public void DateWatched_InTheFuture_IsRejected()
        {
            var dto = new CreateMovieWatchDto { MovieId = 1, DateWatched = DateTime.UtcNow.AddDays(1) };

            Validate(dto).Should().Contain(r => r.MemberNames.Contains(nameof(CreateMovieWatchDto.DateWatched)));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        public void Rating_WithinOneToFive_IsValid(int rating)
        {
            var dto = new CreateMovieWatchDto { MovieId = 1, DateWatched = DateTime.UtcNow.AddDays(-1), Rating = rating };

            Validate(dto).Should().BeEmpty();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        public void Rating_OutsideOneToFive_IsRejected(int rating)
        {
            var dto = new CreateMovieWatchDto { MovieId = 1, DateWatched = DateTime.UtcNow.AddDays(-1), Rating = rating };

            Validate(dto).Should().Contain(r => r.MemberNames.Contains(nameof(CreateMovieWatchDto.Rating)));
        }

        [Fact]
        public void Notes_WithinMaxLength_IsValid()
        {
            var dto = new CreateMovieWatchDto { MovieId = 1, DateWatched = DateTime.UtcNow.AddDays(-1), Notes = new string('a', 500) };

            Validate(dto).Should().BeEmpty();
        }

        [Fact]
        public void Notes_ExceedingMaxLength_IsRejected()
        {
            var dto = new CreateMovieWatchDto { MovieId = 1, DateWatched = DateTime.UtcNow.AddDays(-1), Notes = new string('a', 501) };

            Validate(dto).Should().Contain(r => r.MemberNames.Contains(nameof(CreateMovieWatchDto.Notes)));
        }
    }
}
