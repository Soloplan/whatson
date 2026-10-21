// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConsoleErrorsTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.Tests
{
  using System;
  using System.IO;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Model;
  using WireMock.RequestBuilders;
  using WireMock.ResponseBuilders;
  using WireMock.Server;

  [TestFixture]
  public class ConsoleErrorsTests
  {
    [TestCase(0)]
    [TestCase(33 * 1024 * 1024)]
    public async Task CompilerErrorsAfterWarningHeavyOutputAreNotLost(int precedingCharacters)
    {
      var log = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Recordings", "compiler-diagnostics.txt"));
      var summary = await JenkinsConsoleErrors.Read(new StringReader(new string(' ', precedingCharacters) + "\n" + log), CancellationToken.None);
      Assert.That(summary.Split(Environment.NewLine), Has.Length.EqualTo(4));
      Assert.That(summary, Does.Contain("ExampleLogEntryBuilder.cs(21,5): CS0111:").And.Contain("'ReadInputHeaders'")
        .And.Contain("ExampleLogEntryBuilder.cs(28,5): CS0111:").And.Contain("'ReadOutputHeaders'")
        .And.Contain("ExampleLogEntryBuilder.cs(35,5): CS0111:").And.Contain("ExampleLogEntryBuilder.cs(42,5): CS0111:")
        .And.Not.Contain("CS1574").And.Not.Contain("Warning(s)").And.Not.Contain("agent"));
    }

    [Test]
    public async Task CompilerErrorsAreCondensedAndDeduplicatedAcrossFrameworks()
    {
      var diagnostic = @"C:\agent\workspace\Example\ExampleView.cs(42,7): error CS1061: 'ExampleView' does not contain a definition for 'missingMember' and no accessible extension method 'missingMember' accepting a first argument could be found (are you missing a using directive or an assembly reference?)";
      var log = string.Join("\n", new[]
      {
        "ERROR: script returned exit code 4",
        "[2025-01-01T12:00:00.000Z] " + diagnostic + @" [C:\agent\Example.csproj::TargetFramework=net472]",
        "[2025-01-01T12:00:00.000Z] " + diagnostic + @" [C:\agent\Example.csproj::TargetFramework=net8.0-windows]",
        "Build FAILED.",
      });

      var summary = await JenkinsConsoleErrors.Read(new StringReader(log), CancellationToken.None);
      Assert.That(summary, Is.EqualTo("ExampleView.cs(42,7): CS1061: 'ExampleView' does not contain a definition for 'missingMember'"));
    }

    [TestCase("[2025-01-01T12:00:00.000Z] \u001b[31mERROR: Dependency unavailable\u001b[0m", "ERROR: Dependency unavailable")]
    [TestCase("  Failed Tests.CalculatesPrice [12 ms]", "Failed Tests.CalculatesPrice [12 ms]")]
    [TestCase("  Skipped Tests.NeedsDatabase [1 ms]", "Skipped Tests.NeedsDatabase [1 ms]")]
    [TestCase("No test is available in Example.Tests.dll.", "No test is available in Example.Tests.dll.")]
    [TestCase("Test host process crashed: connection lost", "Test host process crashed: connection lost")]
    [TestCase("Unhandled exception. System.InvalidOperationException: Broken", "Unhandled exception. System.InvalidOperationException: Broken")]
    [TestCase("java.lang.IllegalStateException: Broken", "java.lang.IllegalStateException: Broken")]
    [TestCase("/workspace/main.cpp:12:7: error: missing semicolon", "main.cpp:12:7: error: missing semicolon")]
    [TestCase("ERROR: script returned exit code 4", "")]
    [TestCase("ERROR: process exited with error code 4", "")]
    [TestCase("build.proj(12,3): error MSB3073: command exited with code 4", "")]
    [TestCase("Build succeeded. 0 Error(s).", "")]
    [TestCase("INFO: Try to find errors with: (?mi)(ERROR: Error during SonarQube Scanner execution)", "")]
    [TestCase(@"INFO: Try to find errors with: (?mi)(hudson\.util\.IOException2: [/\\: ]*)(org\.tmatesoft\.svn\.core\.SVNAuthenticationException: [/\\:! ]*)", "")]
    [TestCase("[2025-01-01T12:00:00.000Z] [Build Failure Analyzer] INFO: Try to find errors with: IOException:.*", "")]
    [TestCase("[INFO] File.cs(42,1): error CS1002: example pattern", "")]
    [TestCase("DEBUG: java.lang.IllegalStateException: example pattern", "")]
    [TestCase("[WARNING] IOException: retry succeeded", "")]
    [TestCase("[Pipeline] echo IOException: example pattern", "")]
    [TestCase("Try to find errors with: java.lang.IllegalStateException: pattern", "")]
    [TestCase("Failed in branch Desktop", "")]
    [TestCase("Failed in branch Server", "")]
    [TestCase("Failed in branch Tools", "")]
    [TestCase("IOException: access denied", "IOException: access denied")]
    [TestCase("ERROR: Error during SonarQube Scanner execution", "ERROR: Error during SonarQube Scanner execution")]
    [TestCase("[ERROR] INFO: invalid value", "[ERROR] INFO: invalid value")]
    public async Task FindsActionableErrorsAndIgnoresExitCodeNoise(string log, string expected)
    {
      Assert.That(await JenkinsConsoleErrors.Read(new StringReader(log), CancellationToken.None), Is.EqualTo(expected));
    }

    [TestCase("[10:12:34.123] [publisher] [ERROR] Upload refused", "[publisher] [ERROR] Upload refused")]
    [TestCase("2025-01-01T12:00:00.000Z my.logger - ERROR: Artifact rejected", "my.logger - ERROR: Artifact rejected")]
    [TestCase("[worker] java.lang.IllegalStateException: Broken state", "[worker] java.lang.IllegalStateException: Broken state")]
    [TestCase("worker - System.IO.FileNotFoundException: Could not load assembly", "worker - System.IO.FileNotFoundException: Could not load assembly")]
    [TestCase("bash: compiler: command not found", "bash: compiler: command not found")]
    [TestCase("Could not restore dependencies", "Could not restore dependencies")]
    [TestCase("Compilation failed because the symbol is ambiguous", "Compilation failed because the symbol is ambiguous")]
    [TestCase("[xUnit.net 00:00:01.12] Example.Tests.Calculates [FAIL]", "[xUnit.net 00:00:01.12] Example.Tests.Calculates [FAIL]")]
    [TestCase("No tests were found in Example.Tests.dll", "No tests were found in Example.Tests.dll")]
    [TestCase("Tests were not executed because discovery failed", "Tests were not executed because discovery failed")]
    [TestCase("Tests were ignored because the environment is unavailable", "Tests were ignored because the environment is unavailable")]
    [TestCase("npm ERR! code ENOENT", "npm ERR! code ENOENT")]
    [TestCase("error[E0308]: mismatched types", "error[E0308]: mismatched types")]
    [TestCase("File.cs(5,1): error CS1525: Invalid regex pattern: x", "File.cs(5,1): CS1525: Invalid regex pattern: x")]
    [TestCase("ERROR: failed to compile regex pattern: broken", "ERROR: failed to compile regex pattern: broken")]
    [TestCase("[ERROR] pattern: rejected", "[ERROR] pattern: rejected")]
    [TestCase("[publisher] [INFO] Looking for errors: IOException: search expression", "")]
    [TestCase("my.logger - INFO: Could not restore dependencies (example pattern)", "")]
    [TestCase("File.cs(5,1): warning CS1574: A cref could not be resolved", "")]
    [TestCase("Tests completed. Failed: 0, Passed: 120", "")]
    [TestCase("[Pipeline] echo missing dependency", "")]
    [TestCase(@"2025/01/01 12:00:00 NOTICE: Config file ""C:\agent\rclone.conf"" not found - using defaults", "")]
    [TestCase("Errors:                 0", "")]
    [TestCase("Stage \"Test and Scan\" skipped due to earlier failure(s)", "")]
    [TestCase("Transferred: 436 / 436, 100%", "")]
    [TestCase("2025/01/01 12:00:00 ERROR: Upload failed: permission denied", "ERROR: Upload failed: permission denied")]
    public async Task HeuristicUsesSeverityAndDiagnosticContextAcrossTools(string log, string expected)
    {
      Assert.That(await JenkinsConsoleErrors.Read(new StringReader(log), CancellationToken.None), Is.EqualTo(expected));
    }

    [Test]
    public async Task TestAssertionDetailsOutrankGenericErrorsWithoutIncludingStackTraces()
    {
      var log = string.Join("\n", Enumerable.Range(0, 8).Select(index => $"ERROR: generic problem {index}"))
        + "\nFailed Tests.Calculates [1 ms]\nError Message:\n\nExpected: 42 but was: 0\nStack Trace:\n  at Example.Tests.Calculates()";
      var summary = await JenkinsConsoleErrors.Read(new StringReader(log), CancellationToken.None);
      Assert.That(summary, Does.StartWith("Failed Tests.Calculates [1 ms]").And.Contain("Error Message: Expected: 42 but was: 0")
        .And.Not.Contain("Stack Trace").And.Not.Contain("at Example.Tests").And.Not.Contain("generic problem 3"));
      Assert.That(summary.Split(Environment.NewLine), Has.Length.EqualTo(5));
    }

    [Test]
    public async Task LargeLogsAndLongLinesStayBoundedAndCompilerErrorsTakePriority()
    {
      var noise = new string('x', (2 * 1024 * 1024) + 1);
      var messages = string.Join("\n", Enumerable.Range(0, 10).Select(index => $"ERROR: dependency {index}"));
      var summary = await JenkinsConsoleErrors.Read(new StringReader(noise + "\n" + messages + "\nFile.cs(42,1): error CS1002: ; expected"), CancellationToken.None);
      Assert.That(summary.Split(Environment.NewLine), Has.Length.EqualTo(5));
      Assert.That(summary, Does.StartWith("File.cs(42,1): CS1002: ; expected").And.Not.Contain("dependency 4").And.Not.Contain("xxx"));
    }

    [TestCase(200, "text/plain", "ERROR: missing dependency", "ERROR: missing dependency")]
    [TestCase(200, "text/plain", "Build succeeded", "")]
    [TestCase(200, "text/html", "ERROR: sign in", null)]
    [TestCase(403, "text/plain", "ERROR: forbidden", null)]
    [TestCase(404, "text/plain", "ERROR: not found", null)]
    public async Task ConsoleRequestsUseTheBuildUrlAndIgnoreUnavailableResponses(int status, string mediaType, string body, string? expected)
    {
      using var server = WireMockServer.Start();
      server.Given(Request.Create().WithPath("/job/Release/42/consoleText").UsingGet())
        .RespondWith(Response.Create().WithStatusCode(status).WithHeader("Content-Type", mediaType).WithBody(body));
      var connector = new JenkinsConnector(new ConnectorConfiguration("Jenkins", "Release", Connector.ServerAddress, server.Urls[0], Connector.ProjectName, "Release"), new JenkinsApi());
      Assert.That(await new JenkinsApi().GetConsoleErrors(connector, 42, CancellationToken.None), Is.EqualTo(expected));
      Assert.That(server.LogEntries, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task ReportsPrioritizeBrokenTestsAndLabelTestsThatDidNotRun()
    {
      using var server = WireMockServer.Start();
      server.Given(Request.Create().WithPath("/job/Release/42/testReport/api/xml").UsingGet())
        .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/xml")
          .WithBody("<failures><case><name>Ignored</name><status>SKIPPED</status></case><case><name>Broken</name><status>FAILED</status></case><case><name>Regression</name><status>REGRESSION</status></case><case><name>Passed</name><status>PASSED</status></case></failures>"));
      var api = new JenkinsApi();
      var connector = new JenkinsConnector(new ConnectorConfiguration("Jenkins", "Release", Connector.ServerAddress, server.Urls[0], Connector.ProjectName, "Release"), api);
      Assert.That(await api.GetFailedTests(connector, 42, CancellationToken.None), Is.EqualTo(string.Join(Environment.NewLine, "Broken", "Regression", "SKIPPED: Ignored")));
      Assert.That(Uri.UnescapeDataString(server.LogEntries.Single().RequestMessage?.Url ?? string.Empty), Does.Contain("status='SKIPPED'").And.Contain("position()<=2"));
    }

    [Test]
    public void ConsoleReadingHonorsCancellation()
    {
      using var cancellation = new CancellationTokenSource();
      cancellation.Cancel();
      Assert.That(async () => await JenkinsConsoleErrors.Read(new StringReader("ERROR: ignored"), cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
    }
  }
}
