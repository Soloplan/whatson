// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConnectorBehaviorTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.Tests
{
  using System;
  using System.Linq;
  using System.Net;
  using System.Threading;
  using System.Threading.Tasks;
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Jenkins.Model;
  using Soloplan.WhatsON.Model;

  [TestFixture]
  [SetUICulture("en-US")]
  public class ConnectorBehaviorTests
  {
    [TestCase("SUCCESS", ObservationState.Success)]
    [TestCase("failure", ObservationState.Failure)]
    [TestCase("UNSTABLE", ObservationState.Unstable)]
    [TestCase("ABORTED", ObservationState.Unknown)]
    [TestCase("NEW_JENKINS_RESULT", ObservationState.Unknown)]
    [TestCase(null, ObservationState.Unknown)]
    [TestCase(" ", ObservationState.Unknown)]
    public async Task CompletedBuildResultsAreMappedWithoutTreatingUnrecognizedResultsAsSuccess(string result, ObservationState expected)
    {
      var api = new FakeJenkinsApi();
      api.JobRequest += (sender, args) => args.Result = new JenkinsJob { LastBuild = new JenkinsBuild { Number = 1, Result = result } };
      var connector = new JenkinsConnector(Configuration(), api);

      await connector.QueryStatus(CancellationToken.None);

      Assert.That(connector.CurrentStatus.State, Is.EqualTo(expected));
      Assert.That(connector.Snapshots.Single().Status.State, Is.EqualTo(expected));
    }

    [Test]
    public async Task BuildingFlagTakesPrecedenceOverAReportedResult()
    {
      var api = new FakeJenkinsApi();
      api.JobRequest += (sender, args) =>
        args.Result = new JenkinsJob { LastBuild = new JenkinsBuild { Number = 1, Building = true, Result = "SUCCESS" } };
      var connector = new JenkinsConnector(Configuration(), api);

      await connector.QueryStatus(CancellationToken.None);

      Assert.That(connector.CurrentStatus.State, Is.EqualTo(ObservationState.Running));
      Assert.That(connector.Snapshots, Is.Empty);
    }

    [Test]
    public async Task BuildTimesAndDurationsKeepJenkinsMillisecondPrecision()
    {
      var api = new FakeJenkinsApi();
      api.JobRequest += (sender, args) => args.Result = new JenkinsJob
      {
        LastBuild = new JenkinsBuild
        {
          Number = 1,
          Result = "SUCCESS",
          DisplayName = "Release #1",
          Description = "Published artifacts",
          Timestamp = 1577880000123,
          Duration = 1234,
          EstimatedDuration = 5678,
        },
      };
      var connector = new JenkinsConnector(Configuration(), api);

      await connector.QueryStatus(CancellationToken.None);

      Assert.Multiple(() =>
      {
        Assert.That(connector.CurrentStatus.Time, Is.EqualTo(new DateTime(2020, 1, 1, 12, 0, 0, 123, DateTimeKind.Utc)));
        Assert.That(connector.CurrentStatus.Time.Kind, Is.EqualTo(DateTimeKind.Utc));
        Assert.That(connector.CurrentStatus.Duration, Is.EqualTo(TimeSpan.FromMilliseconds(1234)));
        Assert.That(connector.CurrentStatus.EstimatedDuration, Is.EqualTo(TimeSpan.FromMilliseconds(5678)));
        Assert.That(connector.CurrentStatus.Details, Is.EqualTo("Published artifacts"));
        Assert.That(connector.CurrentStatus.Url, Is.EqualTo("https://builds.example/job/Release/1"));
      });
    }

    [Test]
    public async Task AuthorsAcrossChangeSetsAreDeduplicatedWithoutDroppingOtherAuthors()
    {
      var alice = new JenkinsUser { FullName = "Alice", AbsoluteUrl = "https://builds.example/user/alice" };
      var bob = new JenkinsUser { FullName = "Bob" };
      var build = ApiHelper.GetBuild(ObservationState.Failure, 1);
      build.Culprits = new[] { bob };
      build.ChangeSets = new[]
      {
        new ChangeSet { ChangeSetItems = new[] { new ChangeSetItem { Author = alice }, new ChangeSetItem { Author = bob } } },
        new ChangeSet { ChangeSetItems = new[] { new ChangeSetItem { Author = new JenkinsUser { FullName = "Alice" } } } },
      };
      var api = new FakeJenkinsApi();
      api.JobRequest += (sender, args) => args.Result = new JenkinsJob { LastBuild = build };
      var connector = new JenkinsConnector(Configuration(), api);

      await connector.QueryStatus(CancellationToken.None);

      var status = (JenkinsStatus)connector.CurrentStatus;
      Assert.That(status.CommittedToThisBuild.Select(author => author.FullName), Is.EquivalentTo(new[] { "Alice", "Bob" }));
      Assert.That(status.CommittedToThisBuild.Single(author => author.FullName == "Alice").AbsoluteUrl, Is.EqualTo(alice.AbsoluteUrl));
      Assert.That(status.Culprits, Is.EqualTo(new[] { bob }));
    }

    [Test]
    public async Task ProjectWithoutBuildsReportsTheProblemWithoutInventingHistory()
    {
      var api = new FakeJenkinsApi();
      api.JobRequest += (sender, args) => args.Result = new JenkinsJob();
      var connector = new JenkinsConnector(Configuration(), api);

      await connector.QueryStatus(CancellationToken.None);

      Assert.That(connector.CurrentStatus.InvalidBuild, Is.True);
      Assert.That(connector.CurrentStatus.ErrorMessage, Is.EqualTo("No builds yet"));
      Assert.That(connector.Snapshots, Is.Empty);
    }

    [TestCase(false, true, "Server not available")]
    [TestCase(true, false, "Project not available")]
    public async Task MissingJobResponseDistinguishesServerAndProjectFailures(bool serverAvailable, bool projectAvailable, string expectedMessage)
    {
      var api = new FakeJenkinsApi();
      api.JobRequest += (sender, args) => args.Result = null;
      var connector = new ReachabilityConnector(api, serverAvailable, projectAvailable);

      await connector.QueryStatus(CancellationToken.None);

      Assert.That(connector.CurrentStatus.InvalidBuild, Is.True);
      Assert.That(connector.CurrentStatus.ErrorMessage, Is.EqualTo(expectedMessage));
      Assert.That(connector.Snapshots, Is.Empty);
    }

    [Test]
    public async Task InitialPollingRetainsOnlyTheFiveNewestCompletedBuilds()
    {
      var api = new FakeJenkinsApi();
      var historyRequests = 0;
      api.JobRequest += (sender, args) => args.Result = ApiHelper.GetProject(ObservationState.Success, 6);
      api.BuildsRequest += (sender, args) =>
      {
        historyRequests++;
        for (var number = 5; number >= 1; number--)
        {
          args.Builds.Add(ApiHelper.GetBuild(ObservationState.Success, number));
        }
      };
      var connector = new JenkinsConnector(Configuration(), api);

      await connector.QueryStatus(CancellationToken.None);
      await connector.QueryStatus(CancellationToken.None);

      Assert.That(connector.Snapshots.Select(s => s.Status.BuildNumber), Is.EqualTo(new[] { 6, 5, 4, 3, 2 }));
      Assert.That(historyRequests, Is.EqualTo(1));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task PollingGapRecoversIntermediateFailuresAndToleratesDeletedBuilds(bool middleBuildDeleted)
    {
      var latestNumber = 10;
      var api = new FakeJenkinsApi();
      api.JobRequest += (sender, args) => args.Result = ApiHelper.GetProject(ObservationState.Running, latestNumber);
      api.BuildsRequest += (sender, args) =>
      {
        for (var number = 9; number >= 5; number--)
        {
          args.Builds.Add(ApiHelper.GetBuild(ObservationState.Success, number));
        }
      };
      api.BuildRequest += (sender, args) => args.Result = middleBuildDeleted && args.BuildNumber == 11
        ? null
        : ApiHelper.GetBuild(args.BuildNumber == 12 ? ObservationState.Failure : ObservationState.Success, args.BuildNumber);
      var connector = new JenkinsConnector(Configuration(), api);
      await connector.QueryStatus(CancellationToken.None);

      latestNumber = 13;
      await connector.QueryStatus(CancellationToken.None);

      Assert.Multiple(() =>
      {
        Assert.That(connector.CurrentStatus.State, Is.EqualTo(ObservationState.Running));
        Assert.That(
          connector.Snapshots.Select(s => s.Status.BuildNumber),
          Is.EqualTo(middleBuildDeleted ? new[] { 12, 10, 9, 8, 7 } : new[] { 12, 11, 10, 9, 8 }));
        Assert.That(connector.Snapshots.First().Status.State, Is.EqualTo(ObservationState.Failure));
        Assert.That(connector.Snapshots.All(s => s.Status.State != ObservationState.Running), Is.True);
      });
    }

    [Test]
    public void CancelledApiRequestDoesNotPublishAPhantomStatus()
    {
      var api = new FakeJenkinsApi();
      api.JobRequest += (sender, args) =>
      {
        args.Result = ApiHelper.GetProject(ObservationState.Success, 1);
        args.ResponseDelay = int.MaxValue;
      };
      var connector = new JenkinsConnector(Configuration(), api);
      using (var cancellation = new CancellationTokenSource())
      {
        cancellation.Cancel();

        Assert.That(async () => await connector.QueryStatus(cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
      }

      Assert.That(connector.CurrentStatus, Is.Null);
      Assert.That(connector.Snapshots, Is.Empty);
    }

    [Test]
    public async Task TransportFailurePreservesTheLastCompletedBuildAndRecoveryStillWorks()
    {
      var shouldFail = false;
      var api = new FakeJenkinsApi();
      api.JobRequest += (sender, args) =>
      {
        if (shouldFail)
        {
          throw new WebException("Build server unavailable");
        }

        args.Result = ApiHelper.GetProject(ObservationState.Success, 1);
      };
      var connector = new JenkinsConnector(Configuration(), api);
      await connector.QueryStatus(CancellationToken.None);
      shouldFail = true;

      Assert.That(async () => await connector.QueryStatus(CancellationToken.None), Throws.TypeOf<WebException>());
      Assert.That(connector.Snapshots.Single().Status.State, Is.EqualTo(ObservationState.Success));
      shouldFail = false;
      await connector.QueryStatus(CancellationToken.None);
      Assert.That(connector.Snapshots, Has.Count.EqualTo(1));
      Assert.That(connector.CurrentStatus.State, Is.EqualTo(ObservationState.Success));
    }

    private static ConnectorConfiguration Configuration() => new ConnectorConfiguration("Jenkins", "Release", Connector.ServerAddress, "https://builds.example", Connector.ProjectName, "Release");

    private class ReachabilityConnector : JenkinsConnector
    {
      private readonly bool serverAvailable;

      private readonly bool projectAvailable;

      public ReachabilityConnector(IJenkinsApi api, bool serverAvailable, bool projectAvailable)
        : base(Configuration(), api)
      {
        this.serverAvailable = serverAvailable;
        this.projectAvailable = projectAvailable;
      }

      public override Task<bool> CheckServerURL() => Task.FromResult(this.serverAvailable);

      public override Task<bool> CheckProjectURL() => Task.FromResult(this.projectAvailable);
    }
  }
}
