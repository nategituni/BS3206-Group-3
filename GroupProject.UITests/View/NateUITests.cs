using NUnit.Framework;

namespace GroupProject.UITests.View;

public class NateUITests : BaseTest
{
	public void NavigateToDashboardPage()
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

		var learnerPageBtn = FindUIElement("LearnerPageBtn");
		Assert.That(learnerPageBtn != null, "Learner Page button not found.");
	}

	[Test]
	public void AllOtherButtonsVisible()
	{
		NavigateToDashboardPage();

		var MyAccountButton = FindUIElement("MyAccountPageBtn");
		var LogoutButton = FindUIElement("LogoutBtn");
		var ChallengesButton = FindUIElement("ChallengesPageBtn");
		var SandboxButton = FindUIElement("SandboxPageBtn");

		Assert.That(MyAccountButton != null, "My Account button not found.");
		Assert.That(LogoutButton != null, "Logout button not found.");
		Assert.That(ChallengesButton != null, "Challenges button not found.");
		Assert.That(SandboxButton != null, "Sandbox button not found.");
	}

	[Test]
	public void LearnerPageTruthTableEntry()
	{
		// Arrange
		var learnerPageBtn = FindUIElement("LearnerPageBtn");
		learnerPageBtn.Click();
		Thread.Sleep(1000);

		var truthTableEntryInput = FindUIElement("TruthTableInputEntryField");
		var truthTableEntryOutput = FindUIElement("TruthTableOutputEntryField");
		var generateTruthTableBtn = FindUIElement("GenerateTruthTableBtn");

		// Act
		truthTableEntryInput.Click();
		truthTableEntryInput.SendKeys("1");
		Thread.Sleep(1000);

		truthTableEntryOutput.Click();
		truthTableEntryOutput.SendKeys("1");
		Thread.Sleep(1000);

		// Assert
		Assert.That(truthTableEntryInput != null, "Truth Table Entry field not found.");
		Assert.That(truthTableEntryOutput != null, "Truth Table Output field not found.");
		Assert.That(truthTableEntryInput.Text == "1", "Truth Table Entry field value is incorrect.");
		Assert.That(truthTableEntryOutput.Text == "1", "Truth Table Output field value is incorrect.");
		Assert.That(generateTruthTableBtn != null, "Generate Truth Table button not found.");

		// Arrange
		generateTruthTableBtn.Click();
		Thread.Sleep(1000);

		var inputHeader = FindUIElement("InputHeader_1");
		var outputHeader = FindUIElement("OutputHeader_1");

		outputHeader.Click();
		Thread.Sleep(1000);

		// Assert
		Assert.That(inputHeader != null, "Input Header not found.");
		Assert.That(outputHeader != null, "Output Header not found.");
		Assert.That(inputHeader.Text == "In 1", "Input Header text is incorrect.");
		Assert.That(outputHeader.Text == "Out 1", "Output Header text is incorrect.");
	}
}