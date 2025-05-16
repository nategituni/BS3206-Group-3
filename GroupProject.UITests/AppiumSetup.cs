﻿using NUnit.Framework;

using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;

namespace GroupProject.UITests;

[SetUpFixture]
public class AppiumSetup
{
	private static AppiumDriver? driver;

	public static AppiumDriver App => driver ?? throw new NullReferenceException("AppiumDriver is null");

	[OneTimeSetUp]
	public void RunBeforeAnyTests()
	{
		// If you started an Appium server manually, make sure to comment out the next line
		// This line starts a local Appium server for you as part of the test run
		AppiumServerHelper.StartAppiumLocalServer();

		#if DEBUG
				const string configuration = "Debug";
		#else
				const string configuration = "Release";
		#endif

		const string testsPath = $@"GroupProject.UITests\bin\{configuration}\net9.0";

		var solutionPath = Environment.CurrentDirectory.Replace(testsPath, string.Empty);

		var path = $@"{solutionPath}GroupProject\bin\{configuration}\net9.0-windows10.0.19041.0\win10-x64\GroupProject.exe";

		var windowsOptions = new AppiumOptions
		{
			// Specify windows as the driver, typically don't need to change this
			AutomationName = "windows",
			// Always Windows for Windows
			PlatformName = "Windows",
			// The identifier of the deployed application to test
			

			App = path,
		};

		// Note there are many more options that you can use to influence the app under test according to your needs

		driver = new WindowsDriver(windowsOptions);
	}

	[OneTimeTearDown]
	public void RunAfterAnyTests()
	{
		driver?.Quit();

		// If an Appium server was started locally above, make sure we clean it up here
		AppiumServerHelper.DisposeAppiumLocalServer();
	}
}