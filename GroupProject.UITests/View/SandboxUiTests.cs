using NUnit.Framework;
using OpenQA.Selenium;

namespace GroupProject.UITests.View
{
    [TestFixture]
    public class SandboxUiTests : BaseTest
    {
        [SetUp]
        public void Setup()
        {
            NavigateToSandboxPage();
        }

        public void NavigateToSandboxPage()
        {
            var emailEntry = FindUIElement("LoginEmailEntryField");
            var passwordEntry = FindUIElement("LoginPasswordEntryField");
            var loginButton = FindUIElement("LoginBtn");

            emailEntry.Click();
            emailEntry.SendKeys("test@user.com");
            Thread.Sleep(1000);

            passwordEntry.Click();
            passwordEntry.SendKeys("Testpassword1!");
            Thread.Sleep(1000);

            loginButton.Click();
            Thread.Sleep(1000);

            var sandboxPageButton = FindUIElement("SandboxPageBtn");
            Assert.That(sandboxPageButton != null, "Sandbox Page button not found.");

            sandboxPageButton.Click();
            Thread.Sleep(1000);
        }

        [Test]
        public void AddInputGate_ShouldAppearInCanvas()
        {
            var inputBtn = FindUIElement("AddGateButton_Input");
            Assert.That(inputBtn != null, "Input button not found");

            inputBtn.Click();
            Thread.Sleep(1000);

            var card = FindUIElement("CardView_1");
            Assert.That(card != null, "Input card not added to canvas");
        }

        [Test]
        public void AddAndGate_ShouldAppearInCanvas()
        {
            var andBtn = FindUIElement("AddGateButton_And");
            Assert.That(andBtn != null, "AND button not found");

            andBtn.Click();
            Thread.Sleep(1000);

            var card = FindUIElement("CardView_2");
            Assert.That(card != null, "AND card not added to canvas");
        }

        [Test]
        public void AddOutputGate_ShouldAppearInCanvas()
        {
            var outputBtn = FindUIElement("AddGateButton_Output");
            Assert.That(outputBtn != null, "Output button not found");

            outputBtn.Click();
            Thread.Sleep(1000);

            var card = FindUIElement("CardView_3");
            Assert.That(card != null, "Output card not added to canvas");
        }

        [Test]
        public void SimulateWithoutConnections_ShouldShowError()
        {
            var simulateBtn = FindUIElement("ToolbarItem_Simulate");
            Assert.That(simulateBtn != null, "Simulate button not found");

            simulateBtn.Click();
            Thread.Sleep(1000);

            var errorDialog =
                App.FindElement(By.XPath("//android.widget.TextView[contains(@text, 'Reshuffling failed')]"));
            Assert.That(errorDialog != null, "Expected simulation error message was not shown.");
        }
    }
}