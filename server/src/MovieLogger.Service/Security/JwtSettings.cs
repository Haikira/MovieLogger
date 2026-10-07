namespace MovieLogger.Service.Security
{
    public class JwtSettings
    {
        public const string SectionName = "Jwt";

        /// <summary>HMAC-SHA256 requires a signing key of at least 256 bits.</summary>
        public const int MinimumKeyBytes = 32;

        public string Key { get; set; } = string.Empty;

        public string Issuer { get; set; } = string.Empty;

        public string Audience { get; set; } = string.Empty;

        public int ExpirationMinutes { get; set; } = 60;
    }
}
