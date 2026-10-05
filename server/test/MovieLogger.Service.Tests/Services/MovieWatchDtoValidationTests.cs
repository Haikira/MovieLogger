using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using MovieLogger.Service.Dtos.MovieWatches;
using MovieLogger.Service.Dtos.Validation;

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
            var dto = new CreateMovieWatchDto { MovieId = 1, DateWatched = DateTime.UtcNow.AddDays(2) };

            Validate(dto).Should().Contain(r => r.MemberNames.Contains(nameof(CreateMovieWatchDto.DateWatched)));
        }

        [Theory]
        // 23:30 UTC on 4 Oct is 00:30 on 5 Oct in the UK (BST), so 5 Oct is "today" there.
        [InlineData("2026-10-04T23:30:00", "2026-10-05", true)]
        // 11:00 UTC on 4 Oct is 00:00 on 5 Oct at UTC+13 (New Zealand daylight time).
        [InlineData("2026-10-04T11:00:00", "2026-10-05", true)]
        // 09:00 UTC on 4 Oct is still 4 Oct everywhere (23:00 at UTC+14), so 5 Oct is in the future.
        [InlineData("2026-10-04T09:00:00", "2026-10-05", false)]
        [InlineData("2026-10-04T23:30:00", "2026-10-06", false)]
        [InlineData("2026-10-04T00:00:00", "2026-10-04", true)]
        public void DateWatched_IsComparedAgainstTheLatestCalendarDateAnywhere(string utcNow, string dateWatched, bool expected)
        {
            var now = DateTime.SpecifyKind(DateTime.Parse(utcNow), DateTimeKind.Utc);

            NotInFutureAttribute.IsNotInFuture(DateTime.Parse(dateWatched), now).Should().Be(expected);
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
