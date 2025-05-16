using GroupProject.Services;
using Xunit;

namespace GroupProject.Tests.Services
{
	public class ValidationHelperTests
	{
		[Theory]
		[InlineData("Password1!", true)]
		[InlineData("Arsenal07!", true)]
		[InlineData("weak", false)]
		[InlineData("NoNum", false)]
		[InlineData("NoSpecial1", false)]
		public void IsValidPassword_Should_Return_Correct_Result(string password, bool expectedResult)
		{
			// Act
			var result = ValidationHelper.IsValidPassword(password);

			// Assert
			Assert.Equal(expectedResult, result);
		}

		[Theory]
		[InlineData("user@example.com", true)]
		[InlineData("user.name@domain.co.uk", true)]
		[InlineData("user@sub.domain.com", true)]
		[InlineData("invalid-email", false)]
		[InlineData("user@.com", false)]
		[InlineData("user@com", false)]
		[InlineData("", false)]
		public void IsValidEmail_Should_Return_Correct_Result(string email, bool expectedResult)
		{
			// Act
			var result = ValidationHelper.IsValidEmail(email);

			// Assert
			Assert.Equal(expectedResult, result);
		}
	}
}
