using GroupProject.Models;
using Xunit;

namespace GroupProject.Tests.Models
{
    public class UserModelTests
    {
        [Fact]
        public void User_Should_Initialize_Correctly()
        {
            // Arrange & Act
            var user = new User
            {
                FullName = "Charlie Example",
                Email = "charlie@example.com",
                PasswordHash = "hashedpassword",
                IsMfaVerified = true,
                Role = "Admin",
                ProfilePicture = "/images/profile.png",
                Bio = "Hello! This is my bio."
            };

            // Assert
            Assert.Equal("Charlie Example", user.FullName);
            Assert.Equal("charlie@example.com", user.Email);
            Assert.Equal("hashedpassword", user.PasswordHash);
            Assert.True(user.IsMfaVerified);
            Assert.Equal("Admin", user.Role);
            Assert.Equal("/images/profile.png", user.ProfilePicture);
            Assert.Equal("Hello! This is my bio.", user.Bio);
        }

        [Fact]
        public void User_Should_Have_Default_Values()
        {
            // Arrange & Act
            var user = new User();

            // Assert
            Assert.Null(user.FullName);
            Assert.Null(user.Email);
            Assert.Null(user.PasswordHash);
            Assert.False(user.IsMfaVerified);
            Assert.Null(user.Role);
            Assert.Null(user.ProfilePicture);
            Assert.Null(user.Bio);
        }
    }
}
