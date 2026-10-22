// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsFailureApiTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Tests
{
  using System;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.Jenkins;
  using Soloplan.WhatsON.Jenkins.GUI;
  using Soloplan.WhatsON.Model;
  using ApiServer = Soloplan.WhatsON.GUI.Tests.ProjectDiscoveryTests.DiscoveryServer;

  [TestFixture]
  public class JenkinsFailureApiTests
  {
    [TestCase(false)]
    [TestCase(true)]
    public async Task ReportsLimitThePreviewButIncludeEveryFailedTestWhenExpanded(bool expanded)
    {
      await using var server = new ApiServer();
      var cases = string.Concat(Enumerable.Range(0, 8).Select(index => $"<case><className>Tests</className><name>Case{index}</name><status>FAILED</status></case>"));
      server.Respond = _ => ("200 OK", "application/xml", "<failures><case><name>Passed</name><status>PASSED</status></case><case><name>Skipped</name><status>SKIPPED</status></case><case><className>Tests</className><name>Regression</name><status>REGRESSION</status></case>" + cases + "</failures>");
      var connector = CreateConnector(server.Address);
      var result = await new JenkinsApi().GetFailedTestsAsync(connector, 42, CancellationToken.None, expanded);

      Assert.That(result?.Split(Environment.NewLine), Has.Length.EqualTo(expanded ? 10 : 5));
      Assert.That(result, Does.StartWith("Tests.Regression").And.Contain("Tests.Case0").And.Not.Contain("Passed"));
      Assert.That(result?.Contains("Case7"), Is.EqualTo(expanded));
      Assert.That(result?.Contains("Skipped"), Is.EqualTo(expanded));
      Assert.That(server.Requests.Single(), Does.StartWith("/job/Release/42/testReport/api/xml?"));
      var request = Uri.UnescapeDataString(server.Requests.Single());
      Assert.That(request, Does.Contain("status='FAILED' or status='REGRESSION'"));
      Assert.That(request.Contains("position()<=5"), Is.EqualTo(!expanded));
      Assert.That(server.Requests.Single(), Does.Not.Contain("stdout").And.Not.Contain("errorStackTrace"));
    }

    [Test]
    public async Task ExpandingTheRunLoadsAllRemainingTestsWithoutChangingThePreview()
    {
      await using var server = new ApiServer();
      server.Respond = path => path.Contains("/testReport/", StringComparison.Ordinal)
        ? ("200 OK", "application/xml", "<failures>" + string.Concat(Enumerable.Range(0, Uri.UnescapeDataString(path).Contains("position()<=5", StringComparison.Ordinal) ? 5 : 8)
          .Select(index => $"<case><className>Tests.LayoutTests</className><name>Case{index}</name><status>FAILED</status></case>")) + "</failures>")
        : ("200 OK", "application/xml", "<jobs><job><name>Release</name><lastBuild><number>42</number><result>UNSTABLE</result><action><failCount>8</failCount></action></lastBuild></job></jobs>");
      var connector = CreateConnector(server.Address);
      await connector.QueryStatusBatchAsync(new[] { connector }, CancellationToken.None);
      Assert.That(connector.CurrentStatus?.State, Is.EqualTo(ObservationState.Unstable));
      Assert.That(connector.CurrentStatus?.FailedTestCount, Is.EqualTo(8));
      var project = new JenkinsProjectViewModel(connector);
      project.CurrentStatus.Update(connector.CurrentStatus);
      await project.CurrentStatus.LoadFailureDetailsAsync(CancellationToken.None);
      var preview = project.CurrentStatus.FailureSummary;
      Assert.That(server.Requests, Has.Count.EqualTo(2));
      await project.CurrentStatus.LoadExpandedFailureDetailsAsync(CancellationToken.None);
      Assert.That(project.CurrentStatus.AdditionalFailureGroups.Single(), Is.EqualTo(Enumerable.Range(2, 6).Select(index => $"Case{index}")));
      Assert.That(project.CurrentStatus.FailureSummary, Is.EqualTo(preview));
      await project.CurrentStatus.LoadExpandedFailureDetailsAsync(CancellationToken.None);
      Assert.That(server.Requests, Has.Count.EqualTo(3), "Reopening the expanded list reuses the run's cached details.");
    }

    [Test]
    public async Task ConsoleCompilerErrorsReachTheExistingFailureSummaryAndNotification()
    {
      await using var server = new ApiServer();
      var diagnostic = @"C:\agent\workspace\ExampleView.cs(42,7): error CS1061: 'ExampleView' does not contain a definition for 'missingMember' and no accessible extension method could be found";
      server.Respond = path => path.EndsWith("/consoleText", StringComparison.Ordinal)
        ? ("200 OK", "text/plain", $"Failed in branch Desktop\nINFO: Try to find errors with: (?mi)(ERROR: Error during SonarQube Scanner execution)\nINFO: Try to find errors with: IOException: pattern\n[2025-01-01T12:00:00.000Z] {diagnostic} [Project.csproj::TargetFramework=net472]\n{diagnostic} [Project.csproj::TargetFramework=net8.0-windows]\nERROR: script returned exit code 4")
        : ("200 OK", "application/xml", "<jobs><job><name>Release</name><lastBuild><number>42</number><result>FAILURE</result></lastBuild></job></jobs>");
      var connector = CreateConnector(server.Address);
      await connector.QueryStatusBatchAsync(new[] { connector }, CancellationToken.None);
      Assert.That(server.Requests, Has.Count.EqualTo(1), "Polling must not fetch the console.");
      var project = new JenkinsProjectViewModel(connector) { Name = "Release" };
      project.CurrentStatus.Update(connector.CurrentStatus);
      await project.CurrentStatus.LoadFailureDetailsAsync(CancellationToken.None);

      const string Expected = "ExampleView.cs(42,7): CS1061: 'ExampleView' does not contain a definition for 'missingMember'";
      Assert.That(project.CurrentStatus.FailureSummary, Is.EqualTo(Expected));
      Assert.That(TrayHandler.GetNotificationDescription(project.CurrentStatus), Does.Contain(Expected).And.Not.Contain("exit code"));
      Assert.That(server.Requests, Has.Count.EqualTo(2));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task LateHoverResponseCannotReplaceAnotherRunsDetails(bool expanded)
    {
      await using var server = new ApiServer();
      var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      server.Respond = path =>
      {
        if (path.EndsWith("/consoleText", StringComparison.Ordinal))
        {
          received.TrySetResult();
          return ("200 OK", "text/plain", "OldFile.cs(1,1): error CS1002: ; expected");
        }

        return ("200 OK", "application/xml", "<jobs><job><name>Release</name><lastBuild><number>42</number><result>FAILURE</result></lastBuild></job></jobs>");
      };
      var connector = CreateConnector(server.Address);
      await connector.QueryStatusBatchAsync(new[] { connector }, CancellationToken.None);
      var project = new JenkinsProjectViewModel(connector);
      project.CurrentStatus.Update(connector.CurrentStatus);
      server.ResponseGate = gate.Task;
      var pending = expanded ? project.CurrentStatus.LoadExpandedFailureDetailsAsync(CancellationToken.None) : project.CurrentStatus.LoadFailureDetailsAsync(CancellationToken.None);
      await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
      Assert.That(project.CurrentStatus.IsLoadingFailureDetails, Is.True);
      project.CurrentStatus.Update(new JenkinsStatus(ObservationState.Failure) { BuildNumber = 43, FailureDetails = "New build failure" });
      gate.SetResult();
      await pending.WaitAsync(TimeSpan.FromSeconds(5));
      Assert.That(project.CurrentStatus.FailureDetails, Is.EqualTo("New build failure"));
      Assert.That(project.CurrentStatus.ExpandedFailureDetails, Is.Null);
      Assert.That(project.CurrentStatus.IsLoadingFailureDetails, Is.False);
    }

    [TestCase("404 Not Found", "application/json", "{}")]
    [TestCase("403 Forbidden", "application/json", "{}")]
    [TestCase("200 OK", "text/html", "<html>Sign in</html>")]
    [TestCase("200 OK", "application/xml", "<invalid")]
    [TestCase("200 OK", "application/xml", "<failures/>")]
    [TestCase("200 OK", "application/xml", "<other><case><name>Wrong wrapper</name><status>FAILED</status></case></other>")]
    [TestCase("200 OK", "application/xml", "<!DOCTYPE failures [<!ENTITY name 'not allowed'>]><failures><case><name>&name;</name><status>FAILED</status></case></failures>")]
    [TestCase("200 OK", "application/json", "{}")]
    public async Task OptionalReportFailuresReturnNoDetails(string status, string mediaType, string body)
    {
      await using var server = new ApiServer();
      server.Respond = _ => (status, mediaType, body);
      Assert.That(await new JenkinsApi().GetFailedTestsAsync(CreateConnector(server.Address), 42, CancellationToken.None), Is.Null);
    }

    [Test]
    public async Task OversizedReportsAreIgnored()
    {
      await using var server = new ApiServer();
      server.Respond = _ => ("200 OK", "application/xml", new string(' ', JenkinsApi.MaximumResponseBytes + 1));
      Assert.That(await new JenkinsApi().GetFailedTestsAsync(CreateConnector(server.Address), 42, CancellationToken.None), Is.Null);
    }

    [Test]
    public async Task CancellingAReportRequestIsNotSwallowed()
    {
      await using var server = new ApiServer();
      var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      server.ResponseGate = new TaskCompletionSource().Task;
      server.Respond = _ =>
      {
        received.TrySetResult();
        return ("200 OK", "application/xml", "<failures/>");
      };
      using var cancellation = new CancellationTokenSource();
      var request = new JenkinsApi().GetFailedTestsAsync(CreateConnector(server.Address), 42, cancellation.Token);
      await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
      cancellation.Cancel();
      Assert.That(async () => await request.WaitAsync(TimeSpan.FromSeconds(5)), Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public async Task XmlPollingCombinesTestCountsAndFailureCausesBeforePublishingTheStatus()
    {
      await using var server = new ApiServer();
      server.Respond = path => path.Contains("/testReport/", StringComparison.Ordinal)
        ? ("200 OK", "application/xml", "<failures><case><className>Tests</className><name>Broken</name><status>FAILED</status></case></failures>")
        : ("200 OK", "application/xml", "<jobs><job><name>Release</name><lastBuild><number>42</number><result>UNSTABLE</result><action/><action><failCount>1</failCount></action><action><foundFailureCause><name>Compiler error</name></foundFailureCause><foundFailureCause><name>Dependency unavailable</name></foundFailureCause></action></lastBuild></job></jobs>");
      var connector = CreateConnector(server.Address);
      await connector.QueryStatusBatchAsync(new[] { connector }, CancellationToken.None);

      Assert.That(connector.CurrentStatus?.State, Is.EqualTo(ObservationState.Unstable));
      Assert.That(connector.CurrentStatus?.FailedTestCount, Is.EqualTo(1));
      Assert.That(server.Requests, Has.Count.EqualTo(1), "Polling must not fetch the test report.");
      var details = await connector.LoadFailureDetailsAsync(connector.CurrentStatus ?? throw new AssertionException("Missing status."), CancellationToken.None);
      Assert.That(details, Does.Contain("Compiler error").And.Contain("Dependency unavailable").And.Contain("Tests.Broken"));
      Assert.That(connector.Snapshots.Single().Status.FailureDetails, Is.EqualTo(connector.CurrentStatus?.FailureDetails));
    }

    [TestCase("404 Not Found", "application/json", "{}")]
    [TestCase("403 Forbidden", "text/html", "<html>Sign in</html>")]
    [TestCase("200 OK", "application/json", "{invalid")]
    [TestCase("200 OK", "application/json", "{\"stages\":42}")]
    public async Task MissingOrMalformedPipelineDataDoesNotInventAnExplanation(string status, string mediaType, string body)
    {
      await using var server = new ApiServer();
      server.Respond = _ => (status, mediaType, body);
      Assert.That(await new JenkinsApi().GetPipelineFailureAsync(CreateConnector(server.Address), 42, CancellationToken.None), Is.Null);
    }

    [Test]
    public async Task PipelineNodeIdsCannotChangeTheRequestedPath()
    {
      await using var server = new ApiServer();
      server.Respond = _ => ("200 OK", "application/json", """{"stages":[{"id":"../../other","name":"Compile","status":"FAILED"}]}""");
      Assert.That(await new JenkinsApi().GetPipelineFailureAsync(CreateConnector(server.Address), 42, CancellationToken.None), Is.EqualTo("Compile: FAILED"));
      Assert.That(server.Requests, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task PipelineDiagnosticsInspectAtMostOneNodeAndThreeStages()
    {
      await using var server = new ApiServer();
      var stages = string.Join(",", Enumerable.Range(1, 10).Select(index => $$"""{"id":"{{index}}","name":"Stage {{index}}","status":"FAILED"}"""));
      server.Respond = path => path.Contains("/execution/node/", StringComparison.Ordinal)
        ? ("404 Not Found", "text/plain", string.Empty)
        : ("200 OK", "application/json", "{\"stages\":[" + stages + "]}");
      var result = await new JenkinsApi().GetPipelineFailureAsync(CreateConnector(server.Address), 42, CancellationToken.None);
      Assert.That(result?.Split(Environment.NewLine), Has.Length.EqualTo(3));
      Assert.That(server.Requests, Has.Count.EqualTo(2));
    }

    private static JenkinsConnector CreateConnector(string address) => new JenkinsConnector(
      new ConnectorConfiguration("Jenkins", "Release", Connector.ServerAddress, address, Connector.ProjectName, "Release"), new JenkinsApi());
  }
}
