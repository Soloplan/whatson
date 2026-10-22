// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsServerPollingTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Tests
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using System.Threading;
  using System.Threading.Tasks;
  using System.Xml.Linq;
  using System.Xml.XPath;
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Jenkins;
  using Soloplan.WhatsON.Localization;
  using Soloplan.WhatsON.Model;
  using ApiServer = Soloplan.WhatsON.GUI.Tests.ProjectDiscoveryTests.DiscoveryServer;

  [TestFixture]
  [NonParallelizable]
  public class JenkinsServerPollingTests
  {
    [TestCase(1)]
    [TestCase(20)]
    public async Task OneHundredProjectsUseBoundedBatchesAndDeepFoldersDoNotExpandTheQuery(int depth)
    {
      await using var server = new ApiServer();
      var clock = new Clock();
      var prefix = string.Concat(Enumerable.Range(1, depth - 1).Select(index => $"Folder{index}/"));
      var number = 6;
      server.Respond = path => Reply(path, Enumerable.Range(0, 100).Select(index => Job($"Project {index}", number)));
      var connectors = Enumerable.Range(0, 100).Select(index => CreateConnector(server.Address + "jenkins/", prefix + $"Project {index}", clock)).ToArray();
      var scheduler = new ObservationScheduler();
      var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      var updates = 0;
      scheduler.StatusQueried += (sender, connector) =>
      {
        if (++updates == 100)
        {
          number = 1000;
          clock.Advance(TimeSpan.FromMinutes(5));
        }
        else if (updates == 200)
        {
          scheduler.Stop(false);
          completed.TrySetResult();
        }
      };
      foreach (var connector in connectors)
      {
        scheduler.Observe(connector, 1);
      }

      try
      {
        scheduler.Start();
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.That(server.Requests.Count, Is.InRange(8, 16));
        Assert.That(server.Requests.All(path => Encoding.UTF8.GetByteCount(new Uri(server.Address.TrimEnd('/') + path).AbsoluteUri) <= JenkinsApi.MaximumUrlBytes), Is.True);
        Assert.That(server.Requests.All(path => Query(path)["tree"].Split("jobs[").Length == 2), Is.True);
        Assert.That(connectors.All(connector => connector.CurrentStatus?.BuildNumber == 1000), Is.True);
        Assert.That(connectors.All(connector => connector.Snapshots.Select(snapshot => snapshot.Status.BuildNumber).SequenceEqual([1000, 999, 998, 997, 996])), Is.True);
      }
      finally
      {
        scheduler.Stop(true);
      }
    }

    [Test]
    public async Task InitialBatchesResumePromptlyAndReturnToNormalPolling()
    {
      await using var server = new ApiServer();
      var clock = new Clock();
      server.Respond = path => Reply(path, Enumerable.Range(0, 25).Select(index => Job($"Project{index}", 1)));
      var connectors = Enumerable.Range(0, 9).SelectMany(folder => Enumerable.Range(0, 25)
        .Select(index => CreateConnector(server.Address, $"Folder{folder}/Project{index}", clock))).ToArray();
      var scheduler = new ObservationScheduler();
      var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      var updates = 0;
      var firstWaveRequests = 0;
      scheduler.StatusQueried += (sender, connector) =>
      {
        if (++updates == 200)
        {
          firstWaveRequests = server.Requests.Count;
        }
        else if (updates == connectors.Length)
        {
          clock.Advance(TimeSpan.FromMinutes(5));
          completed.TrySetResult();
        }
      };
      foreach (var connector in connectors)
      {
        scheduler.Observe(connector);
      }

      try
      {
        scheduler.Start();
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.That(firstWaveRequests, Is.EqualTo(JenkinsApi.MaximumRequestsPerPoll));
        Assert.That(connectors.All(connector => connector.CurrentStatus?.BuildNumber == 1), Is.True);
        await Task.Delay(TimeSpan.FromMilliseconds(1500));
        Assert.That(server.Requests, Has.Count.EqualTo(9), "Finishing the first load must restore the normal polling pause.");
      }
      finally
      {
        scheduler.Stop(true);
      }
    }

    [Test]
    public async Task OnlySelectedNamesAreReturnedAndQuotedUnicodeNamesCannotChangeTheFilter()
    {
      await using var server = new ApiServer();
      var names = new[] { "Release", "A'B\"C & # / ü", "編譯 " + new string('ü', 80) };
      var jobs = names.Append("Unrelated").Select(name => Job(name, 6)).ToList();
      var returned = new List<string>();
      server.Respond = path =>
      {
        var response = Reply(path, jobs);
        returned.AddRange(XElement.Parse(response.Body).Elements("job").Select(job => (job.Element("name") ?? throw new AssertionException("Missing job name.")).Value));
        return response;
      };
      var connectors = names.Select(name => CreateConnector(server.Address, name.Replace("/", "%2F"))).ToArray();

      // The stored slash is an encoded part of a branch name, rather than another folder.
      (jobs[1].Element("name") ?? throw new AssertionException("Missing job name.")).Value = names[1].Replace("/", "%2F");

      await connectors[0].QueryStatusBatchAsync(connectors, CancellationToken.None);

      Assert.That(connectors.All(connector => connector.CurrentStatus?.BuildNumber == 6), Is.True);
      Assert.That(returned, Does.Not.Contain("Unrelated"));
      Assert.That(returned.Count, Is.EqualTo(3));
      Assert.That(server.Requests.All(path => Encoding.UTF8.GetByteCount(new Uri(server.Address.TrimEnd('/') + path).AbsoluteUri) <= JenkinsApi.MaximumUrlBytes), Is.True);
    }

    [Test]
    public async Task MissingAndBuildlessProjectsDoNotTriggerIndividualProbesOrHistoryCalls()
    {
      await using var server = new ApiServer();
      server.Respond = path => Reply(path, [Job("Present", 10), new XElement("job", new XElement("name", "No builds"))]);
      var present = CreateConnector(server.Address, "Present");
      var missing = CreateConnector(server.Address, "Missing");
      var buildless = CreateConnector(server.Address, "No builds");
      var queried = await present.QueryStatusBatchAsync([present, missing, buildless], CancellationToken.None);

      Assert.That(queried, Has.Count.EqualTo(3));
      Assert.That(present.CurrentStatus?.BuildNumber, Is.EqualTo(10));
      Assert.That(missing.CurrentStatus?.ErrorMessage, Is.EqualTo(Strings.ProjectNotAvailable));
      Assert.That(buildless.CurrentStatus?.ErrorMessage, Is.EqualTo(Strings.NoBuildsYet));
      Assert.That(server.Requests, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task LargeRunningQueuesStillMakeProgressOnEveryPreviouslyUnseenJob()
    {
      await using var server = new ApiServer();
      var clock = new Clock();
      var jobs = Enumerable.Range(0, 1000).Select(index => Job($"Project{index}", 1, true, clock.GetUtcNow())).ToList();
      server.Respond = path => Reply(path, jobs);
      var connectors = Enumerable.Range(0, 1000).Select(index => CreateConnector(server.Address, $"Project{index}", clock)).ToArray();
      var firstUpdates = 0;
      for (var cycle = 0; cycle < 25 && connectors.Any(connector => connector.CurrentStatus == null); cycle++)
      {
        var previous = server.Requests.Count;
        var updated = await connectors[0].QueryStatusBatchAsync(connectors, CancellationToken.None);
        if (updated.Contains(connectors[0]))
        {
          firstUpdates++;
        }

        Assert.That(server.Requests.Count - previous, Is.LessThanOrEqualTo(JenkinsApi.MaximumRequestsPerPoll));
        clock.Advance(TimeSpan.FromSeconds(10));
      }

      Assert.That(connectors.All(connector => connector.CurrentStatus != null), Is.True, "Oldest idle/unknown jobs must get turns even when the active queue exceeds the request budget.");
      Assert.That(firstUpdates, Is.GreaterThan(1));
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task OversizedBodiesUseCompactStatusEvenWithoutContentLengthOrAfterDecompression(bool chunked, bool gzip)
    {
      await using var server = new ApiServer { Chunked = chunked, Gzip = gzip };
      server.Respond = path => Query(path)["tree"].Contains("culprits", StringComparison.Ordinal)
        ? ("200 OK", "application/xml", new string('x', JenkinsApi.MaximumResponseBytes + 1))
        : Reply(path, [Job("Release", 2)]);
      var connector = CreateConnector(server.Address, "Release");

      await connector.QueryStatusBatchAsync([connector], CancellationToken.None);

      Assert.That(connector.CurrentStatus?.BuildNumber, Is.EqualTo(2));
      Assert.That(server.Requests, Has.Count.EqualTo(2));
    }

    [TestCase("413 Content Too Large")]
    [TestCase("414 URI Too Long")]
    [TestCase("431 Request Header Fields Too Large")]
    public async Task ServerSizeRejectionsSplitTheBatchAndRespectTheAttemptBudget(string status)
    {
      await using var server = new ApiServer();
      server.Respond = path => new XDocument(new XElement("hudson", Job("First", 1), Job("Second", 2))).XPathSelectElements(Query(path)["xpath"]).Count() > 1
        ? (status, "text/plain", "Too large") : Reply(path, [Job("First", 1), Job("Second", 2)]);
      var first = CreateConnector(server.Address, "First");
      var second = CreateConnector(server.Address, "Second");

      await first.QueryStatusBatchAsync([first, second], CancellationToken.None);

      Assert.That(first.CurrentStatus?.BuildNumber, Is.EqualTo(1));
      Assert.That(second.CurrentStatus?.BuildNumber, Is.EqualTo(2));
      Assert.That(server.Requests, Has.Count.EqualTo(4));
    }

    [Test]
    public async Task CompactFallbackRefreshesDescriptionAndPreservesOtherKnownDetails()
    {
      await using var server = new ApiServer();
      var clock = new Clock();
      var rich = Job("Release", 7);
      (rich.Element("lastBuild") ?? throw new AssertionException("Missing last build.")).Add(
        new XElement("description", "Known build details"),
        new XElement("displayName", "Release #7"),
        new XElement("culprit", new XElement("fullName", "Anna")));
      server.Respond = path => Reply(path, [rich]);
      var connector = CreateConnector(server.Address, "Release", clock);
      await connector.QueryStatusBatchAsync([connector], CancellationToken.None);
      clock.Advance(TimeSpan.FromMinutes(5));
      var compact = Job("Release", 7);
      (compact.Element("lastBuild") ?? throw new AssertionException("Missing last build.")).Add(new XElement("description", "Updated build details"));
      server.Respond = path => Query(path)["tree"].Contains("culprits", StringComparison.Ordinal)
        ? ("200 OK", "application/xml", new string('x', JenkinsApi.MaximumResponseBytes + 1)) : Reply(path, [compact]);

      await connector.QueryStatusBatchAsync([connector], CancellationToken.None);
      clock.Advance(TimeSpan.FromMinutes(5));
      await connector.QueryStatusBatchAsync([connector], CancellationToken.None);

      Assert.That(connector.CurrentStatus?.Details, Is.EqualTo("Updated build details"));
      Assert.That((connector.CurrentStatus as JenkinsStatus ?? throw new AssertionException("Expected a JenkinsStatus.")).DisplayName, Is.EqualTo("Release #7"));
      Assert.That((connector.CurrentStatus as JenkinsStatus ?? throw new AssertionException("Expected a JenkinsStatus.")).Culprits.Single().FullName, Is.EqualTo("Anna"));
      Assert.That(server.Requests, Has.Count.EqualTo(4), "The following poll should reuse the compact form instead of downloading oversized details again.");
    }

    [TestCase("503 Service Unavailable", "text/plain", "Unavailable")]
    [TestCase("200 OK", "text/html", "<html>Sign in</html>")]
    [TestCase("200 OK", "application/xml", "<!DOCTYPE jobs [<!ENTITY e SYSTEM 'file:///missing'>]><jobs>&e;</jobs>")]
    [TestCase("200 OK", "application/xml", "<html/>")]
    public async Task FailedPollsPreserveStatusAndRetryAfterBoundedBackoff(string status, string contentType, string body)
    {
      await using var server = new ApiServer();
      var clock = new Clock();
      var connector = CreateConnector(server.Address, "Release", clock);
      server.Respond = path => Reply(path, [Job("Release", 1)]);
      await connector.QueryStatusBatchAsync([connector], CancellationToken.None);
      var previous = connector.CurrentStatus;
      clock.Advance(TimeSpan.FromMinutes(5));
      server.Respond = path => (status, contentType, body);

      Assert.That(await connector.QueryStatusBatchAsync([connector], CancellationToken.None), Is.Empty);
      Assert.That(connector.CurrentStatus, Is.SameAs(previous));
      Assert.That(await connector.QueryStatusBatchAsync([connector], CancellationToken.None), Is.Empty);
      Assert.That(server.Requests, Has.Count.EqualTo(2));
      clock.Advance(TimeSpan.FromSeconds(10));
      server.Respond = path => Reply(path, [Job("Release", 2)]);
      await connector.QueryStatusBatchAsync([connector], CancellationToken.None);

      Assert.That(connector.CurrentStatus?.BuildNumber, Is.EqualTo(2));
      Assert.That(server.Requests, Has.Count.EqualTo(3));
    }

    [TestCase("application/xml")]
    [TestCase("Application/XML; charset=utf-8")]
    public async Task XmlBuildDetailsPreserveCulpritsAndChangeSetAuthors(string mediaType)
    {
      await using var server = new ApiServer();
      server.Respond = path => ("200 OK", mediaType, "<jobs><job><name>Release</name><disabled>true</disabled><lastBuild><number>7</number><building>false</building><result>FAILURE</result><timestamp>1000000</timestamp><duration>12345</duration><displayName>#7</displayName><description>Compiler error</description><culprit><fullName>Anna</fullName><absoluteUrl>https://example.invalid/anna</absoluteUrl></culprit><changeSet><item><author><fullName>Ben</fullName></author></item></changeSet></lastBuild></job></jobs>");
      var connector = CreateConnector(server.Address, "Release");

      await connector.QueryStatusBatchAsync([connector], CancellationToken.None);

      var status = connector.CurrentStatus as JenkinsStatus ?? throw new AssertionException("Expected a JenkinsStatus.");
      Assert.That(connector.IsDisabled, Is.True);
      Assert.That(status.State, Is.EqualTo(ObservationState.Failure));
      Assert.That(status.Details, Is.EqualTo("Compiler error"));
      Assert.That(status.Culprits.Single().FullName, Is.EqualTo("Anna"));
      Assert.That(status.CommittedToThisBuild.Single().FullName, Is.EqualTo("Ben"));
    }

    [Test]
    public async Task UnrequestableUrlsDoNotBlockOtherJobsOrProduceOversizedRequests()
    {
      await using var server = new ApiServer();
      server.Respond = path => Reply(path, [Job("Valid", 1)]);
      var invalid = CreateConnector(server.Address, new string('ü', 1000) + "/Release");
      var valid = CreateConnector(server.Address, "Valid");

      var queried = await valid.QueryStatusBatchAsync([invalid, valid], CancellationToken.None);

      Assert.That(queried, Is.EquivalentTo([valid]));
      Assert.That(invalid.CurrentStatus, Is.Null);
      Assert.That(valid.CurrentStatus?.BuildNumber, Is.EqualTo(1));
      Assert.That(server.Requests, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task VeryLongJobNamesUseShortFiltersAndStillRequireAnExactResponseMatch()
    {
      await using var server = new ApiServer();
      var name = new string('ü', 1000);
      server.Respond = path => Reply(path, [Job(name, 7), Job(name + "unrelated", 99)]);
      var connector = CreateConnector(server.Address, name);

      await connector.QueryStatusBatchAsync([connector], CancellationToken.None);

      Assert.That(connector.CurrentStatus?.BuildNumber, Is.EqualTo(7));
      Assert.That(Encoding.UTF8.GetByteCount(new Uri(server.Address.TrimEnd('/') + server.Requests.Single()).AbsoluteUri), Is.LessThanOrEqualTo(JenkinsApi.MaximumUrlBytes));
    }

    [Test]
    public async Task AnAlwaysOversizedEndpointCannotExceedTheRequestBudgetOrForgetDeferredJobs()
    {
      await using var server = new ApiServer();
      var clock = new Clock();
      var connectors = Enumerable.Range(0, 100).Select(index => CreateConnector(server.Address, $"Project{index}", clock)).ToArray();
      server.Respond = path => ("413 Content Too Large", "text/plain", "Too large");
      Assert.That(await connectors[0].QueryStatusBatchAsync(connectors, CancellationToken.None), Is.Empty);
      Assert.That(server.Requests.Count, Is.LessThanOrEqualTo(JenkinsApi.MaximumRequestsPerPoll));
      Assert.That(connectors.All(connector => connector.CurrentStatus == null), Is.True);

      server.Respond = path => Reply(path, Enumerable.Range(0, 100).Select(index => Job($"Project{index}", 1)));
      await connectors[0].QueryStatusBatchAsync(connectors, CancellationToken.None);
      clock.Advance(TimeSpan.FromSeconds(10));
      await connectors[0].QueryStatusBatchAsync(connectors, CancellationToken.None);

      Assert.That(connectors.All(connector => connector.CurrentStatus?.BuildNumber == 1), Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task SameJobNamesInSeparateFoldersRemainIndependentEvenWhenOneFolderFails(bool firstFails)
    {
      await using var server = new ApiServer();
      server.Respond = path => firstFails && path.StartsWith("/job/First/", StringComparison.Ordinal)
        ? ("403 Forbidden", "text/plain", "Forbidden")
        : Reply(path, [Job("Release", path.StartsWith("/job/First/", StringComparison.Ordinal) ? 1 : 2)]);
      var first = CreateConnector(server.Address, "First/Release");
      var second = CreateConnector(server.Address, "Second/job/Release");
      var queried = await first.QueryStatusBatchAsync([first, second], CancellationToken.None);

      Assert.That(queried.Count, Is.EqualTo(firstFails ? 1 : 2));
      Assert.That(first.CurrentStatus?.BuildNumber, Is.EqualTo(firstFails ? (int?)null : 1));
      Assert.That(second.CurrentStatus?.BuildNumber, Is.EqualTo(2));
      Assert.That(server.Requests, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task CancellingTheHttpRequestDoesNotPublishPartialOrMissingStatuses()
    {
      await using var server = new ApiServer();
      var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      server.ResponseGate = new TaskCompletionSource().Task;
      server.Respond = path =>
      {
        received.TrySetResult();
        return Reply(path, [Job("Release", 1)]);
      };
      var connector = CreateConnector(server.Address, "Release");
      using var cancellation = new CancellationTokenSource();
      var query = connector.QueryStatusBatchAsync([connector], cancellation.Token);
      await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
      cancellation.Cancel();

      Assert.That(async () => await query.WaitAsync(TimeSpan.FromSeconds(5)), Throws.InstanceOf<OperationCanceledException>());
      Assert.That(connector.CurrentStatus, Is.Null);
    }

    [Test]
    public async Task DifferentServersAreStillPolledSeparately()
    {
      await using var first = new ApiServer();
      await using var second = new ApiServer();
      first.Respond = path => Reply(path, [Job("Release", 1)]);
      second.Respond = path => Reply(path, [Job("Release", 2)]);
      var connectors = new[] { CreateConnector(first.Address, "Release"), CreateConnector(second.Address, "Release") };
      var scheduler = new ObservationScheduler();
      var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      var updates = 0;
      scheduler.StatusQueried += (sender, connector) =>
      {
        if (Interlocked.Increment(ref updates) == 2)
        {
          scheduler.Stop(false);
          completed.TrySetResult();
        }
      };
      foreach (var connector in connectors)
      {
        scheduler.Observe(connector, 3600);
      }

      try
      {
        scheduler.Start();
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.That(connectors.Select(connector => connector.CurrentStatus?.BuildNumber), Is.EqualTo([1, 2]));
        Assert.That(first.Requests, Has.Count.EqualTo(1));
        Assert.That(second.Requests, Has.Count.EqualTo(1));
      }
      finally
      {
        scheduler.Stop(true);
      }
    }

    private static JenkinsConnector CreateConnector(string address, string project, Clock? clock = null) => new JenkinsConnector(
      new ConnectorConfiguration("Jenkins", project, Connector.ServerAddress, address, Connector.ProjectName, project), new JenkinsApi(), clock ?? (TimeProvider)TimeProvider.System);

    private static Dictionary<string, string> Query(string path) => new Uri("http://localhost" + path).Query.TrimStart('?').Split('&')
      .Select(part => part.Split('=', 2)).ToDictionary(part => Uri.UnescapeDataString(part[0]), part => Uri.UnescapeDataString(part[1]));

    private static (string Status, string ContentType, string Body) Reply(string path, IEnumerable<XElement> jobs)
    {
      var document = new XDocument(new XElement("hudson", jobs));
      return ("200 OK", "application/xml", new XElement("jobs", document.XPathSelectElements(Query(path)["xpath"])).ToString(SaveOptions.DisableFormatting));
    }

    private static XElement Job(string name, int number, bool running = false, DateTimeOffset? now = null)
    {
      XElement Build(string element, int build) => new XElement(
        element,
        new XElement("number", build),
        new XElement("building", running && build == number),
        new XElement("result", build == number - 1 ? "FAILURE" : "SUCCESS"),
        new XElement("timestamp", now?.ToUnixTimeMilliseconds() ?? build * 60000L),
        new XElement("estimatedDuration", 30000));
      return new XElement(
        "job",
        new XElement("name", name),
        new XElement("disabled", false),
        Build("lastBuild", number),
        Enumerable.Range(Math.Max(1, number - 5), Math.Min(6, number)).Reverse().Select(build => Build("build", build)));
    }

    private sealed class Clock : TimeProvider
    {
      private DateTimeOffset now = DateTimeOffset.UtcNow;

      public override DateTimeOffset GetUtcNow() => this.now;

      public void Advance(TimeSpan interval) => this.now += interval;
    }
  }
}
