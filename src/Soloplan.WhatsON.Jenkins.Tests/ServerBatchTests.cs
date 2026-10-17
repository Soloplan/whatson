// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ServerBatchTests.cs" company="Soloplan GmbH">
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
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Jenkins.Model;
  using Soloplan.WhatsON.Model;

  [TestFixture]
  public class ServerBatchTests
  {
    [Test]
    public async Task MalformedBuildDataDoesNotPreventOtherProjectsFromUpdating()
    {
      var api = new FakeJenkinsApi();
      api.PollingQuery = (connectors, token) =>
      {
        var result = new JenkinsPollingResult();
        foreach (var connector in connectors)
        {
          var build = ApiHelper.GetBuild(ObservationState.Success, 3);
          if (connector.Project == "Broken")
          {
            build.Timestamp = long.MaxValue;
          }

          result.Jobs[connector] = new JenkinsJob { LastBuild = build };
        }

        return Task.FromResult(result);
      };
      var first = CreateConnector(api, "Broken");
      var second = CreateConnector(api, "Working");
      var queried = await first.QueryStatusBatch([first, second], CancellationToken.None);

      Assert.That(queried, Is.EquivalentTo([second]));
      Assert.That(first.CurrentStatus, Is.Null);
      Assert.That(second.CurrentStatus?.BuildNumber, Is.EqualTo(3));
    }

    [Test]
    public async Task CancellingTheSharedRequestDoesNotPublishPartialProjectStatuses()
    {
      var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      var api = new FakeJenkinsApi();
      api.PollingQuery = async (connectors, token) =>
      {
        started.TrySetResult();
        await Task.Delay(Timeout.Infinite, token);
        throw new AssertionException("Cancellation should stop the pending query.");
      };
      var connectors = Enumerable.Range(0, 100).Select(index => CreateConnector(api, $"Project{index}")).ToArray();
      using var cancellation = new CancellationTokenSource();
      var query = connectors[0].QueryStatusBatch(connectors, cancellation.Token);
      await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
      cancellation.Cancel();

      Assert.That(async () => await query, Throws.InstanceOf<OperationCanceledException>());
      Assert.That(connectors.All(connector => connector.CurrentStatus == null && connector.Snapshots.Count == 0), Is.True);
    }

    [Test]
    public async Task FailedAndBudgetDeferredJobsAreNotConfusedWithMissingJobs()
    {
      var clock = new Clock();
      var api = new FakeJenkinsApi();
      var attempted = false;
      api.PollingQuery = (connectors, token) =>
      {
        var result = new JenkinsPollingResult();
        if (!attempted)
        {
          result.Failed.Add(connectors.First());
          attempted = true;
        }
        else
        {
          foreach (var connector in connectors)
          {
            result.Jobs[connector] = new JenkinsJob { LastBuild = ApiHelper.GetBuild(ObservationState.Success, 1) };
          }
        }

        return Task.FromResult(result);
      };
      var first = CreateConnector(api, "Failed", clock);
      var second = CreateConnector(api, "Deferred", clock);
      var batch = new Connector[] { first, second };
      Assert.That(await first.QueryStatusBatch(batch, CancellationToken.None), Is.Empty);
      Assert.That(first.CurrentStatus, Is.Null);
      Assert.That(second.CurrentStatus, Is.Null);

      Assert.That(await first.QueryStatusBatch(batch, CancellationToken.None), Is.EquivalentTo([second]));
      clock.Advance(TimeSpan.FromSeconds(10));
      Assert.That(await first.QueryStatusBatch(batch, CancellationToken.None), Is.EquivalentTo([first]));
    }

    [Test]
    public async Task OlderConcurrentBuildsAreRecordedWhenTheyFinishWithoutANewerBuildStarting()
    {
      var clock = new Clock();
      var api = new FakeJenkinsApi();
      var running = true;
      api.PollingQuery = (connectors, token) =>
      {
        var result = new JenkinsPollingResult();
        foreach (var connector in connectors)
        {
          result.Jobs[connector] = new JenkinsJob
          {
            LastBuild = ApiHelper.GetBuild(ObservationState.Success, 8),
            Builds = [.. Enumerable.Range(3, 6).Reverse().Select(number => ApiHelper.GetBuild(number == 7 && running ? ObservationState.Running : ObservationState.Success, number))],
          };
        }

        return Task.FromResult(result);
      };
      var project = CreateConnector(api, "Release", clock);
      var batch = new Connector[] { project };
      await project.QueryStatusBatch(batch, CancellationToken.None);
      Assert.That(project.Snapshots.Select(snapshot => snapshot.Status.BuildNumber), Is.EqualTo([8, 6, 5, 4, 3]));
      running = false;
      clock.Advance(TimeSpan.FromMinutes(5));
      await project.QueryStatusBatch(batch, CancellationToken.None);

      Assert.That(project.Snapshots.Select(snapshot => snapshot.Status.BuildNumber), Is.EqualTo([8, 7, 6, 5, 4]));
    }

    [TestCase(true, 1, 30000, 0, false, 10)]
    [TestCase(true, 1, 600000, 0, false, 30)]
    [TestCase(true, 9, 600000, 0, false, 10)]
    [TestCase(true, 1, 30000, 0, true, 10)]
    [TestCase(false, 1, 10000, 10000, false, 10)]
    [TestCase(false, 20, 10000, 10000, false, 10)]
    [TestCase(false, 20, 600000, 600000, false, 30)]
    [TestCase(false, 60, 600000, 600000, false, 60)]
    [TestCase(false, 3000, 600000, 600000, false, 300)]
    [TestCase(false, 1, 10000, 10000, true, 300)]
    public void PollingUsesActivityDurationAndCompletionEstimates(bool running, int minutesAgo, int estimate, int duration, bool disabled, int seconds)
    {
      var now = DateTimeOffset.UtcNow;
      var policy = new JenkinsPollingPolicy();
      policy.Succeeded(
        new JenkinsJob
        {
          Disabled = disabled,
          LastBuild = new JenkinsBuild { Building = running, Timestamp = now.AddMinutes(-minutesAgo).ToUnixTimeMilliseconds(), Duration = duration, EstimatedDuration = estimate },
        },
        now);

      Assert.That((policy.NextDue - now).TotalSeconds, Is.EqualTo(seconds));
      Assert.That(policy.NextDue - now, Is.LessThanOrEqualTo(JenkinsPollingPolicy.MaximumInterval));
    }

    [Test]
    public void ARegularlyScheduledJobIsCheckedFrequentlyNearItsExpectedNextRun()
    {
      var now = DateTimeOffset.UtcNow;
      var history = Enumerable.Range(1, 5).Select(index => new JenkinsBuild { Timestamp = now.AddHours(-index * 3).ToUnixTimeMilliseconds(), Duration = 600000 }).ToList();
      var policy = new JenkinsPollingPolicy();
      policy.Succeeded(new JenkinsJob { LastBuild = history.First(), Builds = history }, now);

      Assert.That(policy.NextDue, Is.EqualTo(now.AddSeconds(10)));
    }

    [Test]
    public void NeverRunMissingDisabledAndFailingJobsRemainScheduled()
    {
      var now = DateTimeOffset.UtcNow;
      var policy = new JenkinsPollingPolicy();
      foreach (var job in new[] { null, new JenkinsJob(), new JenkinsJob { Disabled = true } })
      {
        policy.Succeeded(job, now);
        Assert.That(policy.NextDue - now, Is.InRange(TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(5)));
      }

      for (var failure = 0; failure < 20; failure++)
      {
        policy.Failed(now);
        Assert.That(policy.NextDue - now, Is.InRange(TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(5)));
        now = policy.NextDue;
      }
    }

    [Test]
    public void QueuedJobsStayResponsiveBeforeTheirFirstBuildStarts()
    {
      var now = DateTimeOffset.UtcNow;
      var policy = new JenkinsPollingPolicy();
      policy.Succeeded(new JenkinsJob { InQueue = true }, now);

      Assert.That(policy.Priority, Is.True);
      Assert.That(policy.NextDue, Is.EqualTo(now.AddSeconds(10)));
    }

    private static JenkinsConnector CreateConnector(IJenkinsApi api, string project, Clock? clock = null) => new JenkinsConnector(
      new ConnectorConfiguration("Jenkins", project, Connector.ServerAddress, "https://builds.example", Connector.ProjectName, project), api, clock ?? (TimeProvider)TimeProvider.System);

    private sealed class Clock : TimeProvider
    {
      private DateTimeOffset now = DateTimeOffset.UtcNow;

      public override DateTimeOffset GetUtcNow() => this.now;

      public void Advance(TimeSpan interval) => this.now += interval;
    }
  }
}
