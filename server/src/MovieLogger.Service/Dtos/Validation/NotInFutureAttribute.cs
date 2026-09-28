using System.ComponentModel.DataAnnotations;

namespace MovieLogger.Service.Dtos.Validation
{
    public class NotInFutureAttribute : ValidationAttribute
    {
        public NotInFutureAttribute()
            : base("{0} cannot be in the future.")
        {
        }

        public override bool IsValid(object? value)
        {
            if (value is not DateTime dateTime)
            {
                return true;
            }

            return dateTime <= DateTime.UtcNow;
        }
    }
}
