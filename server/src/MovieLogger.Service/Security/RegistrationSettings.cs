namespace MovieLogger.Service.Security
{
    public class RegistrationSettings
    {
        public const string SectionName = "Registration";

        /// <summary>
        /// Whether new accounts can be created. Turning it off (Registration__Enabled=false) closes sign-up on a
        /// public deployment without a code change; existing users can still log in.
        /// </summary>
        public bool Enabled { get; set; } = true;
    }
}
