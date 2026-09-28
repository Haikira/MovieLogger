using System.Security.Claims;

namespace MovieLogger.Api.Security
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetUserId(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.Parse(value ?? throw new InvalidOperationException("The current principal has no NameIdentifier claim."));
        }

        public static int? GetUserIdOrDefault(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : null;
        }
    }
}
