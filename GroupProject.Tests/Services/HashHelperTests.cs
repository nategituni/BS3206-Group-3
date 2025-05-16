using GroupProject.Services;
using Xunit;

namespace GroupProject.Tests.Services
{
    public class HashHelperTests
    {
        [Fact]
        public void HashPassword_Should_Generate_Unique_Hash_For_Same_Input()
        {
            // Arrange
            string password = "SecureP@ssw0rd";

            // Act
            string hash1 = HashHelper.HashPassword(password);
            string hash2 = HashHelper.HashPassword(password);

            // Assert
            Assert.NotEqual(hash1, hash2); // Salts are random → hashes should differ
        }

        [Fact]
        public void VerifyPassword_Should_Return_True_For_Correct_Password()
        {
            // Arrange
            string password = "SecureP@ssw0rd";
            string hashed = HashHelper.HashPassword(password);

            // Act
            bool result = HashHelper.VerifyPassword(password, hashed);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void VerifyPassword_Should_Return_False_For_Incorrect_Password()
        {
            // Arrange
            string correctPassword = "CorrectP@ss123";
            string wrongPassword = "WrongP@ss321";
            string hashed = HashHelper.HashPassword(correctPassword);

            // Act
            bool result = HashHelper.VerifyPassword(wrongPassword, hashed);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("not.base64")]
        [InlineData("too.many.parts.extra")]
        public void VerifyPassword_Should_Return_False_For_Invalid_Hash_Format(string invalidHash)
        {
            // Arrange
            string password = "P@ssword123";

            // Act
            bool result = HashHelper.VerifyPassword(password, invalidHash);

            // Assert
            Assert.False(result);
        }
    }
}
