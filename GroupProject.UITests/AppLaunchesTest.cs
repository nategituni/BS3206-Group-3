using NUnit.Framework;

namespace GroupProject.UITests;

public class MainPageTests : BaseTest
{
	[Test]
	public void AppLaunches()
	{
		App.GetScreenshot().SaveAsFile($"{nameof(AppLaunches)}.png");
	}
}