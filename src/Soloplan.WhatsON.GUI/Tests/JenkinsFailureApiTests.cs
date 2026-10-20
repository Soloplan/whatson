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
  using Soloplan.WhatsON.Jenkins;
  using Soloplan.WhatsON.Model;
  using ApiServer = Soloplan.WhatsON.GUI.Tests.ProjectDiscoveryTests.DiscoveryServer;

  [TestFixture]
  public class JenkinsFailureApiTests
  {
    [Test]
    public async Task ReportsFindFailedAndRegressedTestsAndLimitNames()
    {
      await using var server = new ApiServer();
      var cases = string.Concat(Enumerable.Range(0, 8).Select(index => $"<case><className>Tests</className><name>Case{index}</name><status>FAILED</status></case>"));
      server.Respond = _ => ("200 OK", "application/xml", "<failures><case><name>Passed</name><status>PASSED</status></case><case><name>Skipped</name><status>SKIPPED</status></case><case><className>Tests</className><name>Regression</name><status>REGRESSION</status></case>" + cases + "</failures>");
      var connector = CreateConnector(server.Address);
      var result = await new JenkinsApi().GetFailedTests(connector, 42, CancellationToken.None);

      Assert.That(result?.Split(Environment.NewLine), Has.Length.EqualTo(5));
      Assert.That(result, Does.StartWith("Tests.Regression").And.Contain("Tests.Case0").And.Not.Contain("Passed").And.Not.Contain("Skipped").And.Not.Contain("Case7"));
      Assert.That(server.Requests.Single(), Does.StartWith("/job/Release/42/testReport/api/xml?"));
      Assert.That(Uri.UnescapeDataString(server.Requests.Single()), Does.Contain("status='FAILED' or status='REGRESSION'").And.Contain("position()<=5"));
      Assert.That(server.Requests.Single(), Does.Not.Contain("stdout").And.Not.Contain("errorStackTrace"));
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
      Assert.That(await new JenkinsApi().GetFailedTests(CreateConnector(server.Address), 42, CancellationToken.None), Is.Null);
    }

    [Test]
    public async Task OversizedReportsAreIgnored()
    {
      await using var server = new ApiServer();
      server.Respond = _ => ("200 OK", "application/xml", new string(' ', JenkinsApi.MaximumResponseBytes + 1));
      Assert.That(await new JenkinsApi().GetFailedTests(CreateConnector(server.Address), 42, CancellationToken.None), Is.Null);
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
      var request = new JenkinsApi().GetFailedTests(CreateConnector(server.Address), 42, cancellation.Token);
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
      await connector.QueryStatusBatch(new[] { connector }, CancellationToken.None);

      Assert.That(connector.CurrentStatus?.State, Is.EqualTo(ObservationState.Unstable));
      Assert.That(connector.CurrentStatus?.FailedTestCount, Is.EqualTo(1));
      Assert.That(connector.CurrentStatus?.FailureDetails, Does.Contain("Compiler error").And.Contain("Dependency unavailable").And.Contain("Tests.Broken"));
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
      Assert.That(await new JenkinsApi().GetPipelineFailure(CreateConnector(server.Address), 42, CancellationToken.None), Is.Null);
    }

    [Test]
    public async Task PipelineNodeIdsCannotChangeTheRequestedPath()
    {
      await using var server = new ApiServer();
      server.Respond = _ => ("200 OK", "application/json", """{"stages":[{"id":"../../other","name":"Compile","status":"FAILED"}]}""");
      Assert.That(await new JenkinsApi().GetPipelineFailure(CreateConnector(server.Address), 42, CancellationToken.None), Is.EqualTo("Compile: FAILED"));
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
      var result = await new JenkinsApi().GetPipelineFailure(CreateConnector(server.Address), 42, CancellationToken.None);
      Assert.That(result?.Split(Environment.NewLine), Has.Length.EqualTo(3));
      Assert.That(server.Requests, Has.Count.EqualTo(2));
    }

    private static JenkinsConnector CreateConnector(string address) => new JenkinsConnector(
      new ConnectorConfiguration("Jenkins", "Release", Connector.ServerAddress, address, Connector.ProjectName, "Release"), new JenkinsApi());
  }
}
