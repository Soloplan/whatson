// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ServerTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.CruiseControl.Tests
{
  using System;
  using System.IO;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.CruiseControl.Model;
  using Soloplan.WhatsON.Model;

  [TestFixture]
  [SetUICulture("en-US")]
  [NonParallelizable]
  [SetCulture("en-US")]
  public class ServerTests
  {
    private string directory;

    private string statusFile;

    private CruiseControlServer server;

    [SetUp]
    public void SetUp()
    {
      this.directory = Path.Combine(Path.GetTempPath(), "whatson-cc-tests-" + Guid.NewGuid());
      Directory.CreateDirectory(this.directory);
      this.statusFile = Path.Combine(this.directory, "XmlStatusReport.aspx");
      this.server = new CruiseControlServer(new Uri(this.directory + Path.DirectorySeparatorChar).AbsoluteUri);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(this.directory, true);

    [Test]
    public async Task XmlStatusReportPreservesProjectIdentityResultAndServer()
    {
      File.WriteAllText(
        this.statusFile,
        "<Projects><Project name='Release &amp; QA' category='Tools' activity='Sleeping' lastBuildStatus='Failure' lastBuildLabel='42' lastBuildTime='2020-10-17T12:00:00' nextBuildTime='2020-10-18T12:00:00' webUrl='https://builds.example/release' serverName='local' description='Compile &amp; test'/></Projects>");

      var projects = await this.server.GetAllProjects();
      var project = projects.CruiseControlProject.Single();

      Assert.Multiple(() =>
      {
        Assert.That(project.Name, Is.EqualTo("Release & QA"));
        Assert.That(project.LastBuildStatus, Is.EqualTo(CcBuildStatus.Failure));
        Assert.That(project.LastBuildLabel, Is.EqualTo("42"));
        Assert.That(project.LastBuildTime, Is.EqualTo(new DateTime(2020, 10, 17, 12, 0, 0)));
        Assert.That(project.ServerName, Is.EqualTo("local"));
        Assert.That(project.Description, Is.EqualTo("Compile & test"));
      });
    }

    [Test]
    public async Task ProjectLookupUsesExactNamesAndReturnsNullForMissingProjects()
    {
      File.WriteAllText(this.statusFile, "<Projects><Project name='Release'/><Project name='Release-Linux'/></Projects>");

      Assert.That((await this.server.GetProjectStatus(CancellationToken.None, "Release-Linux", int.MaxValue)).Name, Is.EqualTo("Release-Linux"));
      Assert.That(await this.server.GetProjectStatus(CancellationToken.None, "Missing", int.MaxValue), Is.Null);
    }

    [Test]
    public async Task ProjectsOnTheSameServerShareTheCachedReportWithinThePollInterval()
    {
      File.WriteAllText(
        this.statusFile,
        "<Projects><Project name='First' lastBuildLabel='1'/><Project name='Second' lastBuildLabel='2'/></Projects>");
      await this.server.GetProjectStatus(CancellationToken.None, "First", int.MaxValue);
      File.WriteAllText(this.statusFile, "<Projects><Project name='Second' lastBuildLabel='99'/></Projects>");

      var second = await this.server.GetProjectStatus(CancellationToken.None, "Second", int.MaxValue);

      Assert.That(second.LastBuildLabel, Is.EqualTo("2"));
    }

    [Test]
    public void ConcurrentServerLookupsShareOneCache()
    {
      var address = new Uri(this.directory + Path.DirectorySeparatorChar).AbsoluteUri;
      var servers = new CruiseControlServer[32];

      Parallel.For(0, servers.Length, index => servers[index] = CruiseControlManager.GetServer(address));

      Assert.That(servers.Distinct().Count(), Is.EqualTo(1));
      Assert.That(CruiseControlManager.GetServer(address, false), Is.SameAs(servers[0]));
    }

    [Test]
    public async Task ExpiredPollIntervalRefreshesTheReport()
    {
      File.WriteAllText(this.statusFile, "<Projects><Project name='Release' lastBuildLabel='1'/></Projects>");
      await this.server.GetProjectStatus(CancellationToken.None, "Release", 0);
      File.WriteAllText(this.statusFile, "<Projects><Project name='Release' lastBuildLabel='2'/></Projects>");
      WaitForClockTick();

      Assert.That((await this.server.GetProjectStatus(CancellationToken.None, "Release", 0)).LastBuildLabel, Is.EqualTo("2"));
    }

    [Test]
    public async Task BrokenReportClearsStaleCacheAndARepairedReportCanBeRead()
    {
      File.WriteAllText(this.statusFile, "<Projects><Project name='Release' lastBuildLabel='1'/></Projects>");
      await this.server.GetProjectStatus(CancellationToken.None, "Release", 0);
      File.WriteAllText(this.statusFile, "<broken");
      WaitForClockTick();

      Assert.That(await this.server.GetProjectStatus(CancellationToken.None, "Release", 0), Is.Null);
      File.WriteAllText(this.statusFile, "<Projects><Project name='Release' lastBuildLabel='2'/></Projects>");
      WaitForClockTick();
      Assert.That((await this.server.GetProjectStatus(CancellationToken.None, "Release", 0)).LastBuildLabel, Is.EqualTo("2"));
    }

    [TestCase("<broken")]
    [TestCase("<NotAStatusReport/>")]
    public void ProjectDiscoveryReportsInvalidXmlInsteadOfReturningAnEmptyServer(string xml)
    {
      File.WriteAllText(this.statusFile, xml);

      Assert.That(async () => await this.server.GetAllProjects(), Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public async Task EmptyServerReportDoesNotInventAProject()
    {
      File.WriteAllText(this.statusFile, "<Projects/>");

      Assert.That(await this.server.GetProjectStatus(CancellationToken.None, "Release", 0), Is.Null);
    }

    [Test]
    public async Task BuildHistoryIgnoresUnrelatedLinksAndRespectsTheRequestedLimit()
    {
      var projectDirectory = Path.Combine(this.directory, "project", "Release");
      Directory.CreateDirectory(projectDirectory);
      var html = "<a href='/help' class='passed'>Help</a><table class='RecentBuildsPanel'>"
                 + "<tr><td><a href='/project/Release/build/three.xml' class='passed'>10/17/2020 12:03:00 PM (3)</a></td></tr>"
                 + "<tr><td><a href='/project/Release/build/two.xml' class='failed'>10/17/2020 12:02:00 PM (2)</a></td></tr>"
                 + "<tr><td><a href='/project/Release/build/one.xml' class='passed'>10/17/2020 12:01:00 PM (1)</a></td></tr></table>";
      File.WriteAllText(Path.Combine(projectDirectory, "ViewAllBuilds.aspx"), html);

      var history = await this.server.GetBuilds("Release", 2);

      Assert.That(history.Select(b => b.BuildNumber), Is.EqualTo(new[] { 3, 2 }));
      Assert.That(history.Select(b => b.Status), Is.EqualTo(new[] { CcBuildStatus.Success, CcBuildStatus.Failure }));
    }

    [TestCase("Success", ObservationState.Success)]
    [TestCase("Failure", ObservationState.Failure)]
    [TestCase("Exception", ObservationState.Failure)]
    [TestCase("Unknown", ObservationState.Unknown)]
    [TestCase("Cancelled", ObservationState.Unknown)]
    public async Task ConnectorMapsCompletedBuildResultsWithoutInventingSuccess(string result, ObservationState expected)
    {
      File.WriteAllText(
        this.statusFile,
        $"<Projects><Project name='Release' activity='Sleeping' lastBuildStatus='{result}' lastBuildLabel='1'><messages/></Project></Projects>");
      var connector = this.CreateConnector();

      await connector.QueryStatus(CancellationToken.None);

      Assert.That(connector.CurrentStatus.State, Is.EqualTo(expected));
      Assert.That(connector.Snapshots.Single().Status.State, Is.EqualTo(expected));
    }

    [Test]
    public async Task BuildingActivityTakesPrecedenceOverTheLastCompletedBuildResult()
    {
      File.WriteAllText(
        this.statusFile,
        "<Projects><Project name='Release' activity='Building' lastBuildStatus='Failure' lastBuildLabel='1'><messages/></Project></Projects>");
      var connector = this.CreateConnector();

      await connector.QueryStatus(CancellationToken.None);

      Assert.That(connector.CurrentStatus.State, Is.EqualTo(ObservationState.Running));
      Assert.That(connector.CurrentStatus.Building, Is.True);
      Assert.That(connector.Snapshots, Is.Empty);
    }

    [TestCase("Pending", true, false)]
    [TestCase("CheckingModifications", false, true)]
    public async Task QueuedAndCheckingProjectsKeepTheLastCompletedResultVisible(string activity, bool pending, bool checking)
    {
      File.WriteAllText(
        this.statusFile,
        $"<Projects><Project name='Release' activity='{activity}' lastBuildStatus='Failure' lastBuildLabel='1'><messages/></Project></Projects>");
      var connector = this.CreateConnector();

      await connector.QueryStatus(CancellationToken.None);

      var status = (CruiseControlStatus)connector.CurrentStatus;
      Assert.That(status.State, Is.EqualTo(ObservationState.Failure));
      Assert.That(status.Pending, Is.EqualTo(pending));
      Assert.That(status.CheckingModifications, Is.EqualTo(checking));
    }

    [Test]
    public async Task ReportsWithoutAnOptionalMessagesElementStillProduceAStatus()
    {
      File.WriteAllText(
        this.statusFile,
        "<Projects><Project name='Release' activity='Sleeping' lastBuildStatus='Success' lastBuildLabel='1'/></Projects>");
      var connector = this.CreateConnector();

      await connector.QueryStatus(CancellationToken.None);

      Assert.That(connector.CurrentStatus.State, Is.EqualTo(ObservationState.Success));
      Assert.That(((CruiseControlStatus)connector.CurrentStatus).Culprits, Is.Empty);
    }

    [Test]
    public async Task CulpritNamesAreTrimmedAndDeduplicatedAcrossMessages()
    {
      var report = "<Projects><Project name='Release' activity='Sleeping' lastBuildStatus='Failure' lastBuildLabel='1'><messages>"
                   + "<message kind='Breakers' text=' Alice, Bob, ,Alice '/><message kind='Breakers' text='Bob, Carol'/></messages></Project></Projects>";
      File.WriteAllText(this.statusFile, report);
      var connector = this.CreateConnector();

      await connector.QueryStatus(CancellationToken.None);

      Assert.That(((CruiseControlStatus)connector.CurrentStatus).Culprits.Select(c => c.Name), Is.EquivalentTo(new[] { "Alice", "Bob", "Carol" }));
    }

    [TestCase("Breakers")]
    [TestCase("FailingTasks")]
    [TestCase("BuildAbortedBy")]
    public async Task FailureMessagesCannotBeDisplayedAsSuccessfulOrUnknownBuilds(string kind)
    {
      File.WriteAllText(
        this.statusFile,
        $"<Projects><Project name='Release' activity='Sleeping' lastBuildStatus='Success' lastBuildLabel='1'><messages><message kind='{kind}' text='Build failed'/></messages></Project></Projects>");
      var connector = this.CreateConnector();

      await connector.QueryStatus(CancellationToken.None);

      Assert.That(connector.CurrentStatus.State, Is.EqualTo(ObservationState.Failure));
    }

    [TestCase("Fixer")]
    [TestCase("BuildStatus")]
    [TestCase("NotDefined")]
    public async Task InformationalMessagesDoNotTurnAnUnknownBuildIntoAFailure(string kind)
    {
      File.WriteAllText(
        this.statusFile,
        $"<Projects><Project name='Release' activity='Sleeping' lastBuildStatus='Unknown' lastBuildLabel='1'><messages><message kind='{kind}' text='Informational message'/></messages></Project></Projects>");
      var connector = this.CreateConnector();

      await connector.QueryStatus(CancellationToken.None);

      Assert.That(connector.CurrentStatus.State, Is.EqualTo(ObservationState.Unknown));
    }

    [Test]
    public async Task ProjectsWithoutBuildsDoNotCreateACompletedSnapshot()
    {
      File.WriteAllText(this.statusFile, "<Projects><Project name='Release'/></Projects>");
      var connector = this.CreateConnector();

      await connector.QueryStatus(CancellationToken.None);

      Assert.That(connector.CurrentStatus.InvalidBuild, Is.True);
      Assert.That(connector.CurrentStatus.ErrorMessage, Is.EqualTo("No builds yet"));
      Assert.That(connector.Snapshots, Is.Empty);
    }

    [TestCase(false, true, "Server not available")]
    [TestCase(true, false, "Project not available")]
    [TestCase(true, true, "Project not available")]
    public async Task UnavailableProjectsAreReportedWithoutCrashingOrCreatingHistory(bool serverAvailable, bool projectAvailable, string expected)
    {
      File.WriteAllText(this.statusFile, "<Projects/>");
      var connector = this.CreateConnector(serverAvailable, projectAvailable);

      await connector.QueryStatus(CancellationToken.None);

      Assert.That(connector.CurrentStatus.InvalidBuild, Is.True);
      Assert.That(connector.CurrentStatus.ErrorMessage, Is.EqualTo(expected));
      Assert.That(connector.Snapshots, Is.Empty);
    }

    [Test]
    public async Task ConnectorCombinesTheLatestBuildWithHistoricalResults()
    {
      File.WriteAllText(
        this.statusFile,
        "<Projects><Project name='Release' activity='Sleeping' lastBuildStatus='Success' lastBuildLabel='3'><messages/></Project></Projects>");
      var projectDirectory = Path.Combine(this.directory, "project", "Release");
      Directory.CreateDirectory(projectDirectory);
      var historyHtml = "<table class='RecentBuildsPanel'>"
                        + "<tr><td><a href='/project/Release/build/two.xml' class='failed'>10/17/2020 12:02:00 PM (2)</a></td></tr>"
                        + "<tr><td><a href='/project/Release/build/one.xml' class='passed'>10/17/2020 12:01:00 PM (1)</a></td></tr></table>";
      File.WriteAllText(Path.Combine(projectDirectory, "ViewAllBuilds.aspx"), historyHtml);
      var connector = this.CreateConnector();

      await connector.QueryStatus(CancellationToken.None);

      Assert.That(connector.Snapshots.Select(s => s.Status.BuildNumber), Is.EqualTo(new[] { 3, 2, 1 }));
      Assert.That(
        connector.Snapshots.Select(s => s.Status.State),
        Is.EqualTo(new[] { ObservationState.Success, ObservationState.Failure, ObservationState.Success }));
    }

    private static void WaitForClockTick()
    {
      // The cache expires only when DateTime.Now strictly exceeds the previous poll time.
      var previousTick = DateTime.Now;
      Assert.That(SpinWait.SpinUntil(() => DateTime.Now > previousTick, TimeSpan.FromSeconds(1)), Is.True, "System clock did not advance.");
    }

    private CruiseControlConnector CreateConnector(bool serverAvailable = true, bool projectAvailable = true)
    {
      var address = new Uri(this.directory + Path.DirectorySeparatorChar).AbsoluteUri;
      var configuration = new ConnectorConfiguration(
        "CruiseControl",
        "Release",
        Connector.ServerAddress,
        address,
        Connector.ProjectName,
        "Release",
        Connector.DirectAddress,
        address);
      return new ReachabilityConnector(configuration, serverAvailable, projectAvailable);
    }

    private class ReachabilityConnector : CruiseControlConnector
    {
      private readonly bool serverAvailable;

      private readonly bool projectAvailable;

      public ReachabilityConnector(ConnectorConfiguration configuration, bool serverAvailable, bool projectAvailable)
        : base(configuration)
      {
        this.serverAvailable = serverAvailable;
        this.projectAvailable = projectAvailable;
      }

      public override Task<bool> CheckServerURL() => Task.FromResult(this.serverAvailable);

      public override Task<bool> CheckProjectURL() => Task.FromResult(this.projectAvailable);
    }
  }
}
