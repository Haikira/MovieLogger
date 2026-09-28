using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using MovieLogger.Service.Dtos.Auth;

namespace MovieLogger.Service.Tests.Services
{
    public class AuthDtoValidationTests
    {
        private static IList<ValidationResult> Validate(RegisterRequestDto dto)
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);
            return results;
        }

        [Fact]
        public void RegisterRequestDto_AllFieldsValid_IsValid()
        {
            var dto = new RegisterRequestDto
            {
                DisplayName = "alice",
                Email = "alice@example.com",
                Password = "Password123",
                ConfirmPassword = "Password123"
            };

            Validate(dto).Should().BeEmpty();
        }

        [Fact]
        public void RegisterRequestDto_InvalidEmail_IsRejected()
        {
            var dto = new RegisterRequestDto
            {
                DisplayName = "alice",
                Email = "not-an-email",
                Password = "Password123",
                ConfirmPassword = "Password123"
            };

            Validate(dto).Should().Contain(r => r.MemberNames.Contains(nameof(RegisterRequestDto.Email)));
        }

        [Fact]
        public void RegisterRequestDto_PasswordTooShort_IsRejected()
        {
            var dto = new RegisterRequestDto
            {
                DisplayName = "alice",
                Email = "alice@example.com",
                Password = "short",
                ConfirmPassword = "short"
            };

            Validate(dto).Should().Contain(r => r.MemberNames.Contains(nameof(RegisterRequestDto.Password)));
        }

        [Fact]
        public void RegisterRequestDto_ConfirmPasswordDoesNotMatch_IsRejected()
        {
            var dto = new RegisterRequestDto
            {
                DisplayName = "alice",
                Email = "alice@example.com",
                Password = "Password123",
                ConfirmPassword = "Password456"
            };

            Validate(dto).Should().Contain(r => r.MemberNames.Contains(nameof(RegisterRequestDto.ConfirmPassword)));
        }
    }
}
