using System.ComponentModel.DataAnnotations;

namespace MovieLogger.Service.Dtos.Validation
{
    /// <summary>
    /// Rejects a calendar date that is after today. Clients send the user's local date (e.g. "2026-10-05"),
    /// which can already be tomorrow in UTC, so a date only counts as "in the future" once it is after
    /// today's date in the furthest-ahead time zone (UTC+14).
    /// </summary>
    public class NotInFutureAttribute : ValidationAttribute
    {
        private static readonly TimeSpan MaxUtcOffset = TimeSpan.FromHours(14);

        public NotInFutureAttribute()
            : base("{0} cannot be in the future.")
        {
        }

        public override bool IsValid(object? value)
        {
            return value is not DateTime dateTime || IsNotInFuture(dateTime, DateTime.UtcNow);
        }

        public static bool IsNotInFuture(DateTime value, DateTime utcNow)
        {
            return value.Date <= (utcNow + MaxUtcOffset).Date;
        }
    }
}
