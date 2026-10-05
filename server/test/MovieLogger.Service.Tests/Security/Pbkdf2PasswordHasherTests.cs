using System.Security.Cryptography;
using FluentAssertions;
using MovieLogger.Service.Security;

namespace MovieLogger.Service.Tests.Security
{
    public class Pbkdf2PasswordHasherTests
    {
        private readonly Pbkdf2PasswordHasher _sut = new();

        [Fact]
        public void HashPassword_ThenVerifyWithSamePassword_ReturnsTrue()
        {
            var hash = _sut.HashPassword("Correct-Horse-9");

            _sut.VerifyPassword("Correct-Horse-9", hash).Should().BeTrue();
        }

        [Fact]
        public void VerifyPassword_WithWrongPassword_ReturnsFalse()
        {
            var hash = _sut.HashPassword("Correct-Horse-9");

            _sut.VerifyPassword("Wrong-Horse-9", hash).Should().BeFalse();
        }

        [Fact]
        public void HashPassword_SamePasswordTwice_UsesDifferentSalts()
        {
            _sut.HashPassword("Correct-Horse-9").Should().NotBe(_sut.HashPassword("Correct-Horse-9"));
        }

        [Fact]
        public void HashPassword_RecordsTheIterationCount()
        {
            _sut.HashPassword("Correct-Horse-9").Split('.')[0].Should().Be("600000");
        }

        [Fact]
        public void VerifyPassword_HashCreatedWithOlderIterationCount_StillVerifies()
        {
            // Same format as hashes created before the iteration count was raised from 100,000.
            var salt = RandomNumberGenerator.GetBytes(16);
            var subkey = Rfc2898DeriveBytes.Pbkdf2("Correct-Horse-9", salt, 100_000, HashAlgorithmName.SHA256, 32);
            var legacyHash = $"100000.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(subkey)}";

            _sut.VerifyPassword("Correct-Horse-9", legacyHash).Should().BeTrue();
            _sut.VerifyPassword("Wrong-Horse-9", legacyHash).Should().BeFalse();
        }

        [Theory]
        [InlineData("")]
        [InlineData("not-a-hash")]
        [InlineData("abc.c2FsdA==.c3Via2V5")]
        [InlineData("1000.not base64.c3Via2V5")]
        public void VerifyPassword_MalformedHash_ReturnsFalse(string storedHash)
        {
            _sut.VerifyPassword("Correct-Horse-9", storedHash).Should().BeFalse();
        }
    }
}
