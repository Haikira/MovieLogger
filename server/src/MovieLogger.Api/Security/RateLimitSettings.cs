namespace MovieLogger.Api.Security
{
    /// <summary>
    /// Per-client-IP request limits (the RateLimiting configuration section). Every limit is a fixed window,
    /// partitioned by the client IP that the forwarded-headers middleware has resolved.
    /// </summary>
    public class RateLimitSettings
    {
        public const string SectionName = "RateLimiting";

        /// <summary>Policy name for POST /api/auth/login.</summary>
        public const string LoginPolicy = "login";

        /// <summary>Policy name for POST /api/auth/register.</summary>
        public const string RegisterPolicy = "register";

        public bool Enabled { get; set; } = true;

        /// <summary>All requests from one client, per minute. Generous: it only stops floods.</summary>
        public int RequestsPerMinute { get; set; } = 300;

        /// <summary>Login attempts from one client per five minutes, to slow down password guessing.</summary>
        public int LoginAttemptsPerFiveMinutes { get; set; } = 10;

        /// <summary>New accounts from one client per hour.</summary>
        public int RegistrationsPerHour { get; set; } = 5;
    }
}
