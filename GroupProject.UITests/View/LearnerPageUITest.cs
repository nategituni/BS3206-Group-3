using NUnit.Framework;

namespace GroupProject.UITests.View;

public class LearnerPageUITest : BaseTest
{

	public void NavigateToLearnerPage()
	{
		var emailEntry = FindUIElement("LoginEmailEntryField");
		var passwordEntry = FindUIElement("LoginPasswordEntryField");
		var loginButton = FindUIElement("LoginBtn");

		emailEntry.Click();
		emailEntry.SendKeys("nategituniversity@gmail.com");
		Thread.Sleep(1000);

		passwordEntry.Click();
		passwordEntry.SendKeys("Password1#");
		Thread.Sleep(1000);

		loginButton.Click();
		Thread.Sleep(1000);

		var learnerPageButton = FindUIElement("LearnerPageBtn");

		Assert.That(learnerPageButton != null, "Learner Page button not found.");

		learnerPageButton.Click();
		Thread.Sleep(1000);
	}


	[Test]
	public void LearnerPageInitialization()
	{

		// Arrange
		NavigateToLearnerPage();

		// Act
		var runLearnerButton = FindUIElement("RunLearnerBtn");

		// Assert
		Assert.That(runLearnerButton != null, "Run Learner button not found.");
	}
}