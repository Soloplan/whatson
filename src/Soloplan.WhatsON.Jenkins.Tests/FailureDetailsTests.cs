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
    [TestCase(ObservationState.Failure)]
    [TestCase(ObservationState.Unstable)]
    [TestCase(ObservationState.Success)]
    [TestCase(ObservationState.Running)]
    [TestCase(ObservationState.Unknown)]
    public async Task PollingNeverRequestsFailureDiagnostics(ObservationState state)
    {
      var build = ApiHelper.GetBuild(state, 42);
      build.Actions = new[] { new JenkinsBuildAction { FailCount = 2 } };
      var api = new FakeJenkinsApi();
      api.JobRequest += (_, args) => args.Result = new JenkinsJob { LastBuild = build, Builds = Array.Empty<JenkinsBuild>() };
      api.FailedTestsRequest = (_, _) => throw new AssertionException("Polling must not load test reports.");
      api.ConsoleErrorsRequest = (_, _) => throw new AssertionException("Polling must not download logs.");
      api.PipelineFailureRequest = (_, _) => throw new AssertionException("Polling must not inspect pipeline nodes.");
      var connector = CreateConnector(api);
      await connector.QueryStatus(CancellationToken.None);
      await connector.QueryStatus(CancellationToken.None);
      Assert.That(connector.CurrentStatus?.State, Is.EqualTo(state));
      Assert.That(connector.CurrentStatus?.FailedTestCount, Is.EqualTo(state is ObservationState.Failure or ObservationState.Unstable ? 2 : 0));
    }

    [Test]
    public async Task HistoricalTestsLoadOnDemandAndNeverDownloadTheConsole()
    {
      var failed = ApiHelper.GetBuild(ObservationState.Unstable, 42);
      failed.Actions = new[] { new JenkinsBuildAction { FailCount = 6, SkipCount = 2000 } };
      var api = new FakeJenkinsApi();
      api.JobRequest += (_, args) => args.Result = new JenkinsJob { LastBuild = ApiHelper.GetBuild(ObservationState.Running, 43), Builds = new[] { failed } };
      var requests = 0;
      api.FailedTestsRequest = (number, _) => Task.FromResult<string?>($"Tests.Build{number}.Case{++requests}");
      api.ConsoleErrorsRequest = (_, _) => throw new AssertionException("A failed-test report already explains this build.");
      var connector = CreateConnector(api);
      await connector.QueryStatus(CancellationToken.None);
      var status = connector.Snapshots.Single().Status;
      Assert.That(requests, Is.Zero);
      Assert.That(status.FailedTestCount, Is.EqualTo(6));
      var details = await connector.LoadFailureDetails(status, CancellationToken.None);
      Assert.That(details, Is.EqualTo("Tests.Build42.Case1"));
      Assert.That(await connector.LoadFailureDetails(status, CancellationToken.None), Is.EqualTo(details));
      Assert.That(requests, Is.EqualTo(1));
      Assert.That(connector.CurrentStatus?.State, Is.EqualTo(ObservationState.Running));
      await connector.QueryStatus(CancellationToken.None);
      Assert.That(connector.Snapshots.Single().Status.FailureDetails, Is.EqualTo(details));
    }

    [Test]
    public async Task UnavailableReportsRetryWithoutLosingUsefulConsoleDetails()
    {
      var api = new FakeJenkinsApi();
      var requests = 0;
      api.FailedTestsRequest = (_, _) => Task.FromResult<string?>(++requests == 1 ? null : "Tests.LateResult");
      api.ConsoleErrorsRequest = (_, _) => Task.FromResult<string?>("Test host process crashed");
      var connector = CreateConnector(api);
      var status = new JenkinsStatus(ObservationState.Unstable) { BuildNumber = 42, FailedTestCount = 1 };
      Assert.That(await connector.LoadFailureDetails(status, CancellationToken.None), Is.EqualTo("Test host process crashed"));
      Assert.That(await connector.LoadFailureDetails(status, CancellationToken.None), Is.EqualTo("Tests.LateResult"));
      Assert.That(await connector.LoadFailureDetails(status, CancellationToken.None), Is.EqualTo("Tests.LateResult"));
      Assert.That(requests, Is.EqualTo(2));
    }

    [Test]
    public async Task ConsoleResultsAreCachedPerRunAndUnavailableRequestsRetry()
    {
      var api = new FakeJenkinsApi();
      var requests = 0;
      api.ConsoleErrorsRequest = (number, _) => Task.FromResult<string?>(++requests == 1 ? null : $"File{number}.cs: CS1002: ; expected");
      var connector = CreateConnector(api);
      var first = new JenkinsStatus(ObservationState.Failure) { BuildNumber = 42 };
      Assert.That(await connector.LoadFailureDetails(first, CancellationToken.None), Is.Empty);
      Assert.That(await connector.LoadFailureDetails(first, CancellationToken.None), Does.Contain("File42.cs"));
      Assert.That(await connector.LoadFailureDetails(first, CancellationToken.None), Does.Contain("File42.cs"));
      Assert.That(requests, Is.EqualTo(2));
      var next = new JenkinsStatus(ObservationState.Failure) { BuildNumber = 43 };
      Assert.That(await connector.LoadFailureDetails(next, CancellationToken.None), Does.Contain("File43.cs").And.Not.Contain("File42.cs"));
      next.State = ObservationState.Success;
      Assert.That(await connector.LoadFailureDetails(next, CancellationToken.None), Is.Null);
      Assert.That(requests, Is.EqualTo(3));
    }

    [Test]
    public async Task ExpandedDetailsHaveTheirOwnCacheWithoutReplacingThePreview()
    {
      var api = new FakeJenkinsApi();
      var requests = 0;
      api.FailedTestsRequest = (_, _) => Task.FromResult<string?>(++requests == 1 ? "Tests.Preview" : "Tests.Preview\nTests.Additional");
      var connector = CreateConnector(api);
      var status = new JenkinsStatus(ObservationState.Unstable) { BuildNumber = 42, FailedTestCount = 8 };
      Assert.That(await connector.LoadFailureDetails(status, CancellationToken.None), Is.EqualTo("Tests.Preview"));
      Assert.That(await connector.LoadFailureDetails(status, CancellationToken.None, true), Does.Contain("Tests.Additional"));
      Assert.That(await connector.LoadFailureDetails(status, CancellationToken.None), Is.EqualTo("Tests.Preview"));
      Assert.That(await connector.LoadFailureDetails(status, CancellationToken.None, true), Does.Contain("Tests.Additional"));
      Assert.That(requests, Is.EqualTo(2));
    }

    [Test]
    public async Task ConcurrentHoversShareOneRequestAndCancellationNeverPoisonsTheCache()
    {
      var api = new FakeJenkinsApi();
      var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      var finish = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
      var requests = 0;
      api.ConsoleErrorsRequest = async (_, token) =>
      {
        requests++;
        entered.TrySetResult();
        return await finish.Task.WaitAsync(token);
      };
      var connector = CreateConnector(api);
      var status = new JenkinsStatus(ObservationState.Failure) { BuildNumber = 42 };
      using var cancellation = new CancellationTokenSource();
      var cancelled = connector.LoadFailureDetails(status, cancellation.Token);
      await entered.Task;
      cancellation.Cancel();
      Assert.That(async () => await cancelled, Throws.InstanceOf<OperationCanceledException>());
      var first = connector.LoadFailureDetails(status, CancellationToken.None);
      var second = connector.LoadFailureDetails(status, CancellationToken.None);
      finish.SetResult("File.cs: CS1002: ; expected");
      Assert.That(await first, Is.EqualTo(await second));
      Assert.That(requests, Is.EqualTo(2));
    }

    [Test]
    public async Task CacheIsBoundedAndLateCountsInvalidateOnlyTheirRun()
    {
      var api = new FakeJenkinsApi();
      var requests = 0;
      api.ConsoleErrorsRequest = (_, _) => Task.FromResult<string?>($"ERROR: problem {++requests}");
      api.FailedTestsRequest = (_, _) => Task.FromResult<string?>("Tests.NewFailure");
      var connector = CreateConnector(api);
      for (var number = 1; number <= Connector.MaxSnapshots + 3; number++)
      {
        await connector.LoadFailureDetails(new JenkinsStatus(ObservationState.Failure) { BuildNumber = number }, CancellationToken.None);
      }

      var first = new JenkinsStatus(ObservationState.Failure) { BuildNumber = 1 };
      await connector.LoadFailureDetails(first, CancellationToken.None);
      Assert.That(requests, Is.EqualTo(Connector.MaxSnapshots + 4));
      first.FailedTestCount = 1;
      Assert.That(await connector.LoadFailureDetails(first, CancellationToken.None), Is.EqualTo("Tests.NewFailure"));
    }

    [Test]
    public async Task EmptyCompletedLogsAreCachedButChangingTheProjectInvalidatesTheResult()
    {
      var requests = 0;
      var api = new FakeJenkinsApi();
      api.ConsoleErrorsRequest = (_, _) =>
      {
        requests++;
        return Task.FromResult<string?>(string.Empty);
      };
      var connector = CreateConnector(api);
      var status = new JenkinsStatus(ObservationState.Failure) { BuildNumber = 42 };
      Assert.That(await connector.LoadFailureDetails(status, CancellationToken.None), Is.Empty);
      Assert.That(await connector.LoadFailureDetails(status, CancellationToken.None), Is.Empty);
      Assert.That(requests, Is.EqualTo(1));
      connector.Configuration.GetConfigurationByKey(Connector.ProjectName).Value = "OtherProject";
      await connector.LoadFailureDetails(status, CancellationToken.None);
      Assert.That(requests, Is.EqualTo(2));
    }

    [Test]
    public async Task SkippedTestsCannotHideCompilationFailures()
    {
      var api = new FakeJenkinsApi();
      api.FailedTestsRequest = (_, _) => Task.FromResult<string?>("SKIPPED: Tests.NeedsEnvironment");
      api.ConsoleErrorsRequest = (_, _) => Task.FromResult<string?>("File.cs: CS0111: duplicate member");
      var connector = CreateConnector(api);
      var status = new JenkinsStatus(ObservationState.Failure) { BuildNumber = 42, SkippedTestCount = 1 };
      Assert.That(await connector.LoadFailureDetails(status, CancellationToken.None), Does.StartWith("File.cs: CS0111:").And.Contain("SKIPPED:"));
    }

    [Test]
    public async Task JsonBuildActionsAllowMissingAndUnrelatedPluginData()
    {
      var job = JsonConvert.DeserializeObject<JenkinsJob>(
        """{"lastBuild":{"number":42,"result":"FAILURE","actions":[{},null,{"foundFailureCauses":[{"name":"Compiler error"},null]}]},"builds":[]}""")
        ?? throw new AssertionException("Missing job.");
      var api = new FakeJenkinsApi();
      api.JobRequest += (_, args) => args.Result = job;
      var connector = CreateConnector(api);
      await connector.QueryStatus(CancellationToken.None);
      Assert.That(connector.CurrentStatus?.FailureDetails, Is.EqualTo("Compiler error"));
      Assert.That(connector.CurrentStatus?.FailedTestCount, Is.Zero);
    }

    private static JenkinsConnector CreateConnector(IJenkinsApi api) => new JenkinsConnector(
      new ConnectorConfiguration("Jenkins", "Release", Connector.ServerAddress, "https://builds.example", Connector.ProjectName, "Release"), api);
  }
}
