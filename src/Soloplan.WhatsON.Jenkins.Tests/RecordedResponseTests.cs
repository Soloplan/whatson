// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RecordedResponseTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.Tests
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using System.Xml.Linq;
  using Newtonsoft.Json;
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Jenkins.Model;
  using Soloplan.WhatsON.Model;
  using WireMock.Matchers;
  using WireMock.RequestBuilders;
  using WireMock.ResponseBuilders;
  using WireMock.Server;
  using WireMock.Settings;
  using WireMock.Types;

  [TestFixture]
  [SetUICulture("en-US")]
  public class RecordedResponseTests
  {
    [TestCase(1, ObservationState.Success)]
    [TestCase(2, ObservationState.Success)]
    [TestCase(3, ObservationState.Failure)]
    [TestCase(4, ObservationState.Unstable)]
    [TestCase(5, ObservationState.Running)]
    [TestCase(6, ObservationState.Unknown)]
    public async Task RecordedXmlPollingMatchesJsonStatusAndHistory(int sample, ObservationState expectedState)
    {
      using var server = WireMockServer.Start();
      var json = ReadRecording($"job-{sample}.json", server.Urls[0]);
      var job = JsonConvert.DeserializeObject<JenkinsJob>(json) ?? throw new AssertionException("Missing recorded job.");
      var project = job.FullName ?? throw new AssertionException("Missing recorded project path.");
      var jsonConnector = CreateConnector(server.Urls[0], project);
      var xmlConnector = CreateConnector(server.Urls[0], project);
      var jsonPath = new Uri(JenkinsApi.UrlHelper.JobRequest(jsonConnector)).AbsolutePath;
      server.Given(Request.Create().WithPath(jsonPath).UsingGet())
        .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody(json));
      StubPolling(server, new[] { xmlConnector }, XDocument.Parse(ReadRecording($"poll-{sample}.xml", server.Urls[0])));

      await jsonConnector.QueryStatus(CancellationToken.None);
      var queried = await xmlConnector.QueryStatusBatch(new[] { xmlConnector }, CancellationToken.None);

      Assert.That(queried, Is.EquivalentTo(new[] { xmlConnector }));
      Assert.That(xmlConnector.CurrentStatus?.State, Is.EqualTo(expectedState));
      Assert.That(xmlConnector.IsDisabled, Is.EqualTo(job.Disabled));
      if (job.LastBuild == null)
      {
        Assert.That(xmlConnector.CurrentStatus?.InvalidBuild, Is.True);
        Assert.That(xmlConnector.CurrentStatus?.ErrorMessage, Is.EqualTo(jsonConnector.CurrentStatus?.ErrorMessage));
        Assert.That(xmlConnector.Snapshots, Is.Empty);
      }
      else
      {
        var expected = jsonConnector.CurrentStatus as JenkinsStatus ?? throw new AssertionException("Missing JSON status.");
        var actual = xmlConnector.CurrentStatus as JenkinsStatus ?? throw new AssertionException("Missing XML status.");
        Assert.That(actual.BuildNumber, Is.EqualTo(job.LastBuild.Number));
        Assert.That(actual.Building, Is.EqualTo(job.LastBuild.Building));
        Assert.That(actual.Time, Is.EqualTo(expected.Time));
        Assert.That(actual.Duration, Is.EqualTo(expected.Duration));
        Assert.That(actual.EstimatedDuration, Is.EqualTo(expected.EstimatedDuration));
        Assert.That(actual.DisplayName, Is.EqualTo(expected.DisplayName));
        Assert.That(actual.Details, Is.EqualTo(expected.Details));
        Assert.That(actual.Culprits.Select(user => user.FullName), Is.EqualTo(expected.Culprits.Select(user => user.FullName)));
        Assert.That(actual.CommittedToThisBuild.Select(user => user.FullName), Is.EqualTo(expected.CommittedToThisBuild.Select(user => user.FullName)));
        var completed = (job.Builds ?? Array.Empty<JenkinsBuild>()).Where(build => !build.Building)
          .OrderByDescending(build => build.Number).Take(Connector.MaxSnapshots).Select(build => build.Number);
        Assert.That(xmlConnector.Snapshots.Select(snapshot => snapshot.Status.BuildNumber), Is.EqualTo(completed));
        Assert.That(
          xmlConnector.Snapshots.Select(snapshot => snapshot.Status.State),
          Is.EqualTo(jsonConnector.Snapshots.Select(snapshot => snapshot.Status.State)));
      }

      Assert.That(server.LogEntries.Count(), Is.EqualTo(2), "Replay must not probe the live server or fetch history separately.");
    }

    [TestCase(301)]
    [TestCase(302)]
    [TestCase(307)]
    public async Task RedirectedPollingPreservesThePreviousJsonApiBehavior(int redirectStatus)
    {
      using var server = WireMockServer.Start();
      var json = ReadRecording("job-1.json", server.Urls[0]);
      var job = JsonConvert.DeserializeObject<JenkinsJob>(json) ?? throw new AssertionException("Missing recorded job.");
      var project = job.FullName ?? throw new AssertionException("Missing recorded project path.");
      var jsonConnector = CreateConnector(server.Urls[0], project);
      var xmlConnector = CreateConnector(server.Urls[0], project);
      server.Given(Request.Create().WithPath(new Uri(JenkinsApi.UrlHelper.JobRequest(jsonConnector)).AbsolutePath).UsingGet())
        .RespondWith(Response.Create().WithStatusCode(redirectStatus).WithHeader("Location", server.Urls[0] + "/canonical/json"));
      server.Given(Request.Create().WithPath(JenkinsApi.PollingRequest(new[] { xmlConnector }, false).AbsolutePath).UsingGet())
        .RespondWith(Response.Create().WithStatusCode(redirectStatus).WithHeader("Location", server.Urls[0] + "/canonical/xml"));
      server.Given(Request.Create().WithPath("/canonical/json").UsingGet())
        .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody(json));
      server.Given(Request.Create().WithPath("/canonical/xml").UsingGet())
        .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/xml")
          .WithBody(ReadRecording("poll-1.xml", server.Urls[0])));

      await jsonConnector.QueryStatus(CancellationToken.None);
      var queried = await xmlConnector.QueryStatusBatch(new[] { xmlConnector }, CancellationToken.None);

      Assert.That(jsonConnector.CurrentStatus?.State, Is.EqualTo(ObservationState.Success));
      Assert.That(queried, Is.EquivalentTo(new[] { xmlConnector }));
      Assert.That(xmlConnector.CurrentStatus?.BuildNumber, Is.EqualTo(jsonConnector.CurrentStatus?.BuildNumber));
      Assert.That(xmlConnector.CurrentStatus?.State, Is.EqualTo(jsonConnector.CurrentStatus?.State));
      Assert.That(server.LogEntries.Count(), Is.EqualTo(4));
    }

    [Test]
    public async Task RecordedDiscoveryPreservesNestedProjectPaths()
    {
      // Commas are part of Jenkins's tree expression, not separate query values.
      using var server = WireMockServer.Start(new WireMockServerSettings { QueryParameterMultipleValueSupport = QueryParameterMultipleValueSupport.None });
      server.Given(Request.Create().WithPath("/api/json").WithParam("tree", new ExactMatcher(JenkinsJobs.RequestProperties)).UsingGet())
        .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
          .WithBody(ReadRecording("discovery.json", server.Urls[0])));

      var projects = await new JenkinsPlugin().GetProjects(server.Urls[0]) ?? throw new AssertionException("Discovery failed.");
      var leaves = Descendants(projects).Where(project => project.Children.Count == 0).ToArray();
      var expectedPaths = Enumerable.Range(1, 6).Select(sample =>
        JsonConvert.DeserializeObject<JenkinsJob>(ReadRecording($"job-{sample}.json", server.Urls[0]))?.FullName);

      Assert.That(leaves.Select(project => project.FullName), Is.EquivalentTo(expectedPaths));
      Assert.That(leaves.All(project => project.Parent != null), Is.True);
      Assert.That(leaves.All(project => project.Address?.StartsWith(server.Urls[0], StringComparison.Ordinal) == true), Is.True);
      Assert.That(server.LogEntries.Count(), Is.EqualTo(1), "Recorded nested jobs should not require individual discovery calls.");
    }

    [Test]
    public async Task RecordedProjectsArePolledTogetherWithinEachFolder()
    {
      using var server = WireMockServer.Start();
      var connectors = Enumerable.Range(1, 6).Select(sample =>
      {
        var job = JsonConvert.DeserializeObject<JenkinsJob>(ReadRecording($"job-{sample}.json", server.Urls[0]));
        return CreateConnector(server.Urls[0], job?.FullName ?? throw new AssertionException("Missing recorded project path."));
      }).ToArray();
      var groups = connectors.GroupBy(JenkinsApi.UrlHelper.ParentUrl).ToArray();
      foreach (var group in groups)
      {
        var jobs = group.Select(connector =>
        {
          var sample = Array.IndexOf(connectors, connector) + 1;
          return XDocument.Parse(ReadRecording($"poll-{sample}.xml", server.Urls[0])).Root?.Element("job")
            ?? throw new AssertionException("Missing recorded XML job.");
        });
        StubPolling(server, group.ToArray(), new XDocument(new XElement("jobs", jobs)));
      }

      var queried = await connectors[0].QueryStatusBatch(connectors, CancellationToken.None);

      Assert.That(queried, Is.EquivalentTo(connectors));
      Assert.That(connectors.Select(connector => connector.CurrentStatus?.State), Is.EqualTo(new ObservationState?[]
      {
        ObservationState.Success, ObservationState.Success, ObservationState.Failure,
        ObservationState.Unstable, ObservationState.Running, ObservationState.Unknown,
      }));
      Assert.That(server.LogEntries.Count(), Is.EqualTo(groups.Length));
    }

    private static JenkinsConnector CreateConnector(string address, string project) => new JenkinsConnector(
      new ConnectorConfiguration("Jenkins", "Recorded project", Connector.ServerAddress, address, Connector.ProjectName, project), new JenkinsApi());

    private static string ReadRecording(string file, string address) =>
      File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Recordings", file))
        .Replace("http://build.example.invalid", address.TrimEnd('/'));

    private static void StubPolling(WireMockServer server, IReadOnlyList<JenkinsConnector> connectors, XDocument response)
    {
      var uri = JenkinsApi.PollingRequest(connectors, false);
      var parameters = uri.Query.TrimStart('?').Split('&').Select(part => part.Split('=', 2))
        .ToDictionary(parts => parts[0], parts => Uri.UnescapeDataString(parts[1]));
      server.Given(Request.Create().WithPath(uri.AbsolutePath).WithParam("tree", parameters["tree"])
          .WithParam("xpath", parameters["xpath"]).WithParam("wrapper", "jobs").UsingGet())
        .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/xml")
          .WithBody(response.ToString(SaveOptions.DisableFormatting)));
    }

    private static IEnumerable<Project> Descendants(IEnumerable<Project> projects) =>
      projects.SelectMany(project => new[] { project }.Concat(Descendants(project.Children)));
  }
}
