// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VolunteerRecoveryTests.cs" company="Soloplan GmbH">
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
  using Newtonsoft.Json;
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Jenkins.Model;
  using Soloplan.WhatsON.Model;

  [TestFixture]
  public class VolunteerRecoveryTests
  {
    private string directory = string.Empty;
    private FakeJenkinsApi api = new FakeJenkinsApi();
    private JenkinsBuild claimed = new JenkinsBuild();
    private JenkinsJob job = new JenkinsJob();
    private int clearAttempts;
    private bool failClear;
    private bool failRead;

    [SetUp]
    public void SetUp()
    {
      this.directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "claims-" + Guid.NewGuid().ToString("N"));
      this.claimed = new JenkinsBuild { Number = 42, Result = "FAILURE", Description = "Release notes" };
      this.job = new JenkinsJob { LastBuild = this.claimed, Builds = Array.Empty<JenkinsBuild>() };
      this.clearAttempts = 0;
      this.failClear = false;
      this.failRead = false;
      this.api = new FakeJenkinsApi();
      this.api.JobRequest += (_, e) => e.Result = this.job;
      this.api.BuildRequest += (_, e) =>
      {
        Assert.That(e.BuildNumber, Is.EqualTo(42));
        e.Result = this.failRead ? null : this.claimed;
      };
      this.api.VolunteerQuery = (_, number, expectedOwner, claim, _) =>
      {
        Assert.That(number, Is.EqualTo(42));
        Assert.That(JenkinsVolunteer.GetOwner(this.claimed.Description), Is.EqualTo(expectedOwner));
        if (!claim)
        {
          this.clearAttempts++;
          if (this.failClear)
          {
            throw new IOException("Temporary write failure");
          }
        }

        this.claimed.Description = JenkinsVolunteer.SetOwner(this.claimed.Description, claim ? "alex" : null);
        return Task.FromResult(this.claimed);
      };
    }

    [TearDown]
    public void TearDown()
    {
      if (Directory.Exists(this.directory))
      {
        Directory.Delete(this.directory, true);
      }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task RecoveryClearsTheOriginalBuildAfterRestartEvenOutsideRecentHistory(bool failedAgain)
    {
      var original = this.CreateConnector();
      await original.SetVolunteer(42, null, true, CancellationToken.None);
      var jobUrl = JenkinsApi.UrlHelper.ProjectUrl(original, false);
      var saved = new JenkinsVolunteerStore(this.directory).Read(jobUrl).Single();
      Assert.That(saved.BuildNumber, Is.EqualTo(42));
      Assert.That(saved.Owner, Is.EqualTo("alex"));
      Assert.That(saved.JobUrl, Is.EqualTo(jobUrl));

      this.job.LastBuild = new JenkinsBuild { Number = 100, Result = failedAgain ? "FAILURE" : "SUCCESS" };
      this.job.LastStableBuild = new JenkinsBuild { Number = failedAgain ? 75 : 100 };
      var restarted = this.CreateConnector();
      await restarted.QueryStatus(CancellationToken.None);

      Assert.That(this.clearAttempts, Is.EqualTo(1));
      Assert.That(this.claimed.Description, Is.EqualTo("Release notes"));
      Assert.That(new JenkinsVolunteerStore(this.directory).Read(jobUrl), Is.Empty);
      Assert.That(restarted.CurrentStatus?.BuildNumber, Is.EqualTo(100));
    }

    [TestCase("FAILURE", false)]
    [TestCase("UNSTABLE", false)]
    [TestCase(null, true)]
    public async Task LaterFailuresAndRunningBuildsDoNotClearTheClaim(string? result, bool building)
    {
      var connector = this.CreateConnector();
      await connector.SetVolunteer(42, null, true, CancellationToken.None);
      this.job.LastBuild = new JenkinsBuild { Number = 43, Result = result, Building = building };
      this.job.LastStableBuild = new JenkinsBuild { Number = 41 };
      await connector.QueryStatus(CancellationToken.None);
      Assert.That(this.clearAttempts, Is.Zero);
      Assert.That(JenkinsVolunteer.GetOwner(this.claimed.Description), Is.EqualTo("alex"));
      Assert.That(new JenkinsVolunteerStore(this.directory).Read(JenkinsApi.UrlHelper.ProjectUrl(connector, false)).Count(), Is.EqualTo(1));
    }

    [TestCase("alex")]
    [TestCase("sam")]
    public async Task OnlyTheClientWithLocalTrackingAutomaticallyClearsTheNotice(string otherUser)
    {
      var creator = this.CreateConnector();
      await creator.SetVolunteer(42, null, true, CancellationToken.None);
      this.job.LastBuild = new JenkinsBuild { Number = 43, Result = "SUCCESS" };
      var otherClient = this.CreateConnector(Path.Combine(this.directory, "other-client"), otherUser);
      await otherClient.QueryStatus(CancellationToken.None);
      Assert.That(this.clearAttempts, Is.Zero, "A matching Jenkins user name alone must not authorize automatic cleanup.");
      Assert.That(JenkinsVolunteer.GetOwner(this.claimed.Description), Is.EqualTo("alex"));

      await this.CreateConnector().QueryStatus(CancellationToken.None);
      Assert.That(this.clearAttempts, Is.EqualTo(1), "The original client resumes its own cleanup after restart.");
      Assert.That(this.claimed.Description, Is.EqualTo("Release notes"));
    }

    [Test]
    public async Task AnotherConfiguredClientCanClearManuallyAndTheCreatorDropsItsTrackingLater()
    {
      var creator = this.CreateConnector();
      await creator.SetVolunteer(42, null, true, CancellationToken.None);
      var otherClient = this.CreateConnector(Path.Combine(this.directory, "other-client"), "sam");
      await otherClient.QueryStatus(CancellationToken.None);
      await otherClient.SetVolunteer(42, JenkinsVolunteer.GetOwner(otherClient.CurrentStatus?.Details), false, CancellationToken.None);
      Assert.That(this.clearAttempts, Is.EqualTo(1));
      Assert.That(this.claimed.Description, Is.EqualTo("Release notes"));
      var jobUrl = JenkinsApi.UrlHelper.ProjectUrl(creator, false);
      Assert.That(new JenkinsVolunteerStore(this.directory).Read(jobUrl).Count(), Is.EqualTo(1));

      this.job.LastBuild = new JenkinsBuild { Number = 43, Result = "SUCCESS" };
      await this.CreateConnector().QueryStatus(CancellationToken.None);
      Assert.That(this.clearAttempts, Is.EqualTo(1), "The creator must not write again after another user cleared the notice.");
      Assert.That(new JenkinsVolunteerStore(this.directory).Read(jobUrl), Is.Empty);
    }

    [TestCase("read")]
    [TestCase("write")]
    [TestCase("credentials")]
    public async Task CleanupKeepsTrackingUntilItCanSucceed(string failure)
    {
      var connector = this.CreateConnector();
      await connector.SetVolunteer(42, null, true, CancellationToken.None);
      this.job.LastBuild = new JenkinsBuild { Number = 43, Result = "SUCCESS" };
      this.failRead = failure == "read";
      this.failClear = failure == "write";
      if (failure == "credentials")
      {
        connector.Configuration.GetConfigurationByKey(JenkinsConnector.ApiToken).Value = string.Empty;
      }

      await connector.QueryStatus(CancellationToken.None);
      Assert.That(connector.CurrentStatus?.State, Is.EqualTo(ObservationState.Success), "Cleanup errors must not hide build recovery.");
      Assert.That(new JenkinsVolunteerStore(this.directory).Read(JenkinsApi.UrlHelper.ProjectUrl(connector, false)).Count(), Is.EqualTo(1));
      this.failRead = false;
      this.failClear = false;
      await this.CreateConnector().QueryStatus(CancellationToken.None);
      Assert.That(this.claimed.Description, Is.EqualTo("Release notes"));
      Assert.That(new JenkinsVolunteerStore(this.directory).Read(JenkinsApi.UrlHelper.ProjectUrl(connector, false)), Is.Empty);
    }

    [Test]
    public async Task JenkinsLastSuccessfulBuildDoesNotTreatUnstableAsRecovery()
    {
      var connector = this.CreateConnector();
      await connector.SetVolunteer(42, null, true, CancellationToken.None);
      this.job = JsonConvert.DeserializeObject<JenkinsJob>(
        "{\"builds\":[],\"lastBuild\":{\"number\":44,\"result\":\"FAILURE\"},\"lastSuccessfulBuild\":{\"number\":43},\"lastStableBuild\":{\"number\":41}}")
        ?? throw new AssertionException("Missing job.");
      await connector.QueryStatus(CancellationToken.None);
      Assert.That(this.clearAttempts, Is.Zero);
      Assert.That(JenkinsVolunteer.GetOwner(this.claimed.Description), Is.EqualTo("alex"));
    }

    [TestCase(null)]
    [TestCase("someone else")]
    public async Task CleanupDoesNotRemoveAReplacementVolunteer(string? owner)
    {
      var connector = this.CreateConnector();
      await connector.SetVolunteer(42, null, true, CancellationToken.None);
      this.claimed.Description = JenkinsVolunteer.SetOwner("Release notes", owner);
      this.job.LastBuild = new JenkinsBuild { Number = 43, Result = "SUCCESS" };
      await connector.QueryStatus(CancellationToken.None);
      Assert.That(this.clearAttempts, Is.Zero);
      Assert.That(JenkinsVolunteer.GetOwner(this.claimed.Description), Is.EqualTo(owner));
      Assert.That(new JenkinsVolunteerStore(this.directory).Read(JenkinsApi.UrlHelper.ProjectUrl(connector, false)), Is.Empty);
    }

    [Test]
    public async Task ManualClearAlsoRemovesTracking()
    {
      var connector = this.CreateConnector();
      await connector.SetVolunteer(42, null, true, CancellationToken.None);
      await connector.SetVolunteer(42, "alex", false, CancellationToken.None);
      Assert.That(new JenkinsVolunteerStore(this.directory).Read(JenkinsApi.UrlHelper.ProjectUrl(connector, false)), Is.Empty);
    }

    [Test]
    public async Task DamagedNoticeKeepsRecoveryTrackingUntilItIsRepaired()
    {
      var connector = this.CreateConnector();
      await connector.SetVolunteer(42, null, true, CancellationToken.None);
      var original = this.claimed.Description ?? throw new AssertionException("Missing notice.");
      this.claimed.Description = original.Replace("YWxleA==", "/w==");
      this.job.LastBuild = new JenkinsBuild { Number = 43, Result = "SUCCESS" };
      await connector.QueryStatus(CancellationToken.None);
      Assert.That(this.clearAttempts, Is.Zero);
      Assert.That(new JenkinsVolunteerStore(this.directory).Read(JenkinsApi.UrlHelper.ProjectUrl(connector, false)).Count(), Is.EqualTo(1));
      this.claimed.Description = original;
      await this.CreateConnector().QueryStatus(CancellationToken.None);
      Assert.That(this.claimed.Description, Is.EqualTo("Release notes"));
    }

    [TestCase("SUCCESS", false, false)]
    [TestCase(null, true, false)]
    [TestCase("SUCCESS", false, true)]
    [TestCase(null, true, true)]
    public async Task ANewerHealthyOrRunningBuildRefreshesWithoutClaiming(string? result, bool building, bool connectorAlreadyUpdated)
    {
      var connector = this.CreateConnector();
      this.job.LastBuild = new JenkinsBuild { Number = 43, Result = result, Building = building };
      if (connectorAlreadyUpdated)
      {
        connector.CurrentStatus = new JenkinsStatus(ObservationState.Failure) { BuildNumber = 43 };
      }

      var updated = await connector.SetVolunteer(42, null, true, CancellationToken.None);
      Assert.That(updated.BuildNumber, Is.EqualTo(43));
      Assert.That(connector.CurrentStatus?.BuildNumber, Is.EqualTo(43));
      Assert.That(connector.CurrentStatus?.Building, Is.EqualTo(building));
      Assert.That(this.claimed.Description, Is.EqualTo("Release notes"));
      Assert.That(Directory.Exists(this.directory), Is.False);
    }

    [TestCase("FAILURE", false)]
    [TestCase("UNSTABLE", false)]
    [TestCase("FAILURE", true)]
    [TestCase("UNSTABLE", true)]
    public async Task AStaleClaimAutomaticallyTargetsTheNewBrokenBuild(string result, bool connectorAlreadyUpdated)
    {
      var connector = this.CreateConnector();
      var latest = new JenkinsBuild { Number = 43, Result = result, Description = "New build notes" };
      this.job.LastBuild = latest;
      if (connectorAlreadyUpdated)
      {
        connector.CurrentStatus = new JenkinsStatus(ObservationState.Failure) { BuildNumber = 43 };
      }

      var writes = 0;
      this.api.VolunteerQuery = (_, number, expectedOwner, claim, _) =>
      {
        Assert.That(number, Is.EqualTo(43));
        Assert.That(expectedOwner, Is.Null);
        Assert.That(claim, Is.True);
        writes++;
        latest.Description = JenkinsVolunteer.SetOwner(latest.Description, "alex");
        return Task.FromResult(latest);
      };

      var updated = await connector.SetVolunteer(42, null, true, CancellationToken.None);

      Assert.That(writes, Is.EqualTo(1));
      Assert.That(updated.BuildNumber, Is.EqualTo(43));
      Assert.That(JenkinsVolunteer.GetOwner(connector.CurrentStatus?.Details), Is.EqualTo("alex"));
      Assert.That(this.claimed.Description, Is.EqualTo("Release notes"));
      Assert.That(new JenkinsVolunteerStore(this.directory).Read(JenkinsApi.UrlHelper.ProjectUrl(connector, false)).Single().BuildNumber, Is.EqualTo(43));
    }

    [Test]
    public async Task ANewerClaimedBuildRefreshesWithoutReplacingItsVolunteer()
    {
      var connector = this.CreateConnector();
      var description = JenkinsVolunteer.SetOwner("New build notes", "sam");
      this.job.LastBuild = new JenkinsBuild { Number = 43, Result = "FAILURE", Description = description };
      this.api.VolunteerQuery = (_, _, _, _, _) => throw new AssertionException("An existing volunteer must not be overwritten.");

      var updated = await connector.SetVolunteer(42, null, true, CancellationToken.None);

      Assert.That(updated.BuildNumber, Is.EqualTo(43));
      Assert.That(connector.CurrentStatus?.Details, Is.EqualTo(description));
      Assert.That(this.claimed.Description, Is.EqualTo("Release notes"));
      Assert.That(Directory.Exists(this.directory), Is.False);
    }

    [Test]
    public async Task TrackingSurvivesAnUncertainWriteResponse()
    {
      var connector = this.CreateConnector();
      var update = this.api.VolunteerQuery ?? throw new AssertionException("Missing fake update.");
      this.api.VolunteerQuery = async (project, number, owner, claim, token) =>
      {
        await update(project, number, owner, claim, token);
        throw new IOException("The server accepted the change but the connection failed.");
      };
      Assert.ThrowsAsync<IOException>(() => connector.SetVolunteer(42, null, true, CancellationToken.None));
      this.api.VolunteerQuery = update;
      this.job.LastBuild = new JenkinsBuild { Number = 43, Result = "SUCCESS" };
      await this.CreateConnector().QueryStatus(CancellationToken.None);
      Assert.That(this.claimed.Description, Is.EqualTo("Release notes"));
    }

    [Test]
    public void TrackingSeparatesServersJobsAndBuilds()
    {
      var store = new JenkinsVolunteerStore(this.directory);
      store.Save("https://one.example/job/a", 42, "alex");
      store.Save("https://one.example/job/a", 43, "alex");
      store.Save("https://two.example/job/a", 42, "other");
      store.Save("https://one.example/job/b", 42, "other");
      var restarted = new JenkinsVolunteerStore(this.directory);
      Assert.That(restarted.Read("https://one.example/job/a").Select(claim => claim.BuildNumber), Is.EquivalentTo(new[] { 42, 43 }));
      restarted.Remove("https://one.example/job/a", 42);
      Assert.That(restarted.Read("https://two.example/job/a").Single().Owner, Is.EqualTo("other"));
      Assert.That(restarted.Read("https://one.example/job/b").Single().Owner, Is.EqualTo("other"));
    }

    private JenkinsConnector CreateConnector(string? trackingDirectory = null, string userName = "alex")
    {
      var configuration = new ConnectorConfiguration("Jenkins", "job", Connector.ServerAddress, "https://builds.example", Connector.ProjectName, "folder/job");
      configuration.GetConfigurationByKey(JenkinsConnector.UserName).Value = userName;
      configuration.GetConfigurationByKey(JenkinsConnector.ApiToken).Value = "encrypted-placeholder";
      return new JenkinsConnector(configuration, this.api, TimeProvider.System, new JenkinsVolunteerStore(trackingDirectory ?? this.directory))
      {
        CurrentStatus = new JenkinsStatus(ObservationState.Failure) { BuildNumber = 42 },
      };
    }
  }
}
