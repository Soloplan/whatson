// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FailureDetailsTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.Tests
{
  using System;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using Newtonsoft.Json;
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Jenkins.Model;
  using Soloplan.WhatsON.Model;

  [TestFixture]
  public class FailureDetailsTests
  {
    [TestCase(ObservationState.Failure, 1)]
    [TestCase(ObservationState.Unstable, 1)]
    [TestCase(ObservationState.Success, 0)]
    [TestCase(ObservationState.Running, 0)]
    [TestCase(ObservationState.Unknown, 0)]
    public async Task OnlyBrokenBuildsLoadTestsAndSuccessfulReportsAreCached(ObservationState state, int expectedRequests)
    {
      var build = ApiHelper.GetBuild(state, 42);
      build.Actions = new[] { new JenkinsBuildAction { FailCount = 2, FoundFailureCauses = new[] { new JenkinsFailureCause { Name = "Compilation failed" } } } };
      var api = new FakeJenkinsApi();
      api.JobRequest += (_, args) => args.Result = new JenkinsJob { LastBuild = build, Builds = Array.Empty<JenkinsBuild>() };
      var requests = 0;
      api.FailedTestsRequest = (_, _) =>
      {
        requests++;
        return Task.FromResult<string?>("Tests.First\nTests.Second");
      };
      var connector = CreateConnector(api);
      await connector.QueryStatus(CancellationToken.None);
      await connector.QueryStatus(CancellationToken.None);

      Assert.That(requests, Is.EqualTo(expectedRequests));
      Assert.That(connector.CurrentStatus?.State, Is.EqualTo(state));
      Assert.That(connector.CurrentStatus?.FailedTestCount, Is.EqualTo(expectedRequests * 2));
      if (expectedRequests > 0)
      {
        Assert.That(connector.CurrentStatus?.FailureDetails, Does.Contain("Compilation failed").And.Contain("Tests.First"));
        Assert.That(connector.Snapshots.Single().Status.FailureDetails, Is.EqualTo(connector.CurrentStatus?.FailureDetails));
      }
      else
      {
        Assert.That(connector.CurrentStatus?.FailureDetails, Is.Null);
      }
    }

    [Test]
    public async Task UnavailableReportsRetryAndNewBuildsNeverReuseOldTestNames()
    {
      var build = ApiHelper.GetBuild(ObservationState.Unstable, 42);
      build.Actions = new[] { new JenkinsBuildAction { FailCount = 1 } };
      var api = new FakeJenkinsApi();
      api.JobRequest += (_, args) => args.Result = new JenkinsJob { LastBuild = build, Builds = new[] { build } };
      var requests = 0;
      api.FailedTestsRequest = (number, _) => Task.FromResult<string?>(++requests == 1 ? null : $"Tests.Build{number}");
      var connector = CreateConnector(api);
      await connector.QueryStatus(CancellationToken.None);
      Assert.That(connector.CurrentStatus?.State, Is.EqualTo(ObservationState.Unstable));
      Assert.That(connector.CurrentStatus?.FailedTestCount, Is.EqualTo(1));
      await connector.QueryStatus(CancellationToken.None);
      Assert.That(connector.Snapshots.Single().Status.FailureDetails, Does.Contain("Tests.Build42"));

      build.Number = 43;
      await connector.QueryStatus(CancellationToken.None);
      Assert.That(connector.CurrentStatus?.FailureDetails, Does.Contain("Tests.Build43").And.Not.Contain("Tests.Build42"));
      Assert.That(connector.Snapshots.Single(snapshot => snapshot.Status.BuildNumber == 42).Status.FailureDetails, Does.Contain("Tests.Build42"));
      Assert.That(requests, Is.EqualTo(3));
    }

    [Test]
    public async Task JsonBuildActionsAllowMissingAndUnrelatedPluginData()
    {
      var job = JsonConvert.DeserializeObject<JenkinsJob>(
        """{"lastBuild":{"number":42,"result":"FAILURE","actions":[{},null,{"foundFailureCauses":[{"name":"Compiler error"},null]}]},"builds":[]}""")
        ?? throw new AssertionException("Missing job.");
      var api = new FakeJenkinsApi();
      api.JobRequest += (_, args) => args.Result = job;
      api.FailedTestsRequest = (_, _) => throw new AssertionException("No failed-test count means no report request.");
      var connector = CreateConnector(api);
      await connector.QueryStatus(CancellationToken.None);

      Assert.That(connector.CurrentStatus?.FailureDetails, Is.EqualTo("Compiler error"));
      Assert.That(connector.CurrentStatus?.FailedTestCount, Is.Zero);
    }

    [Test]
    public async Task PipelineFallbackDoesNotPreventLateTestReportsAndRecoveryClearsDetails()
    {
      var build = ApiHelper.GetBuild(ObservationState.Unstable, 42);
      build.Actions = new[] { new JenkinsBuildAction { FailCount = 1 } };
      var api = new FakeJenkinsApi();
      api.JobRequest += (_, args) => args.Result = new JenkinsJob { LastBuild = build, Builds = new[] { build } };
      var testRequests = 0;
      var pipelineRequests = 0;
      api.FailedTestsRequest = (_, _) => Task.FromResult<string?>(++testRequests == 1 ? null : "Tests.LateResult");
      api.PipelineFailureRequest = (_, _) =>
      {
        pipelineRequests++;
        return Task.FromResult<string?>("Test stage: UNSTABLE");
      };
      var connector = CreateConnector(api);
      await connector.QueryStatus(CancellationToken.None);
      Assert.That(connector.CurrentStatus?.FailureDetails, Is.EqualTo("Test stage: UNSTABLE"));
      await connector.QueryStatus(CancellationToken.None);
      Assert.That(connector.CurrentStatus?.FailureDetails, Is.EqualTo("Tests.LateResult"));
      Assert.That(connector.Snapshots.Single().Status.FailureDetails, Is.EqualTo("Tests.LateResult"));
      build = ApiHelper.GetBuild(ObservationState.Success, 43);
      await connector.QueryStatus(CancellationToken.None);
      Assert.That(connector.CurrentStatus?.FailureDetails, Is.Null);
      Assert.That(pipelineRequests, Is.EqualTo(1));
      Assert.That(testRequests, Is.EqualTo(2));
    }

    [Test]
    public async Task PipelineFailuresWithoutTestActionsAreCachedPerBuild()
    {
      var build = ApiHelper.GetBuild(ObservationState.Failure, 42);
      var api = new FakeJenkinsApi();
      api.JobRequest += (_, args) => args.Result = new JenkinsJob { LastBuild = build, Builds = new[] { build } };
      var requests = 0;
      api.PipelineFailureRequest = (number, _) =>
      {
        requests++;
        return Task.FromResult<string?>($"Stage {number}: script returned exit code 1");
      };
      var connector = CreateConnector(api);
      await connector.QueryStatus(CancellationToken.None);
      await connector.QueryStatus(CancellationToken.None);
      Assert.That(requests, Is.EqualTo(1));
      Assert.That(connector.CurrentStatus?.FailureDetails, Does.Contain("Stage 42"));
      build = ApiHelper.GetBuild(ObservationState.Failure, 43);
      await connector.QueryStatus(CancellationToken.None);
      Assert.That(requests, Is.EqualTo(2));
      Assert.That(connector.CurrentStatus?.FailureDetails, Does.Contain("Stage 43").And.Not.Contain("Stage 42"));
    }

    private static JenkinsConnector CreateConnector(IJenkinsApi api) => new JenkinsConnector(
      new ConnectorConfiguration("Jenkins", "Release", Connector.ServerAddress, "https://builds.example", Connector.ProjectName, "Release"), api);
  }
}
