using NUnit.Framework;

namespace GroupProject.UITests.View;

public class ChallengesPageUITest : BaseTest
{
    public void NavigateToChallengesPage()
    {
        var emailEntry = FindUIElement("LoginEmailEntryField");
        var passwordEntry = FindUIElement("LoginPasswordEntryField");
        var loginButton = FindUIElement("LoginBtn");

        emailEntry.Click();
        emailEntry.SendKeys("michael.eddleston@icloud.com");
		Thread.Sleep(1000);

        passwordEntry.Click();
        passwordEntry.SendKeys("Password1!");
        Thread.Sleep(1000);

        loginButton.Click();
        Thread.Sleep(1000);

        var ChallengesPageButton = FindUIElement("ChallengesPageBtn");

        Assert.That(ChallengesPageButton != null, "Challenges Page button not found.");

        ChallengesPageButton.Click();
        Thread.Sleep(1000);
    }

    [Test]
    public void ChallengesPageInitialiation()
    {
		NavigateToChallengesPage();

        var challengeText = FindUIElement("ChallengeText");

		Assert.That(challengeText != null, "Challenges not found.");
    }

}