// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsObservedFailureTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Tests
{
  using System;
  using System.IO;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using Newtonsoft.Json;
  using Newtonsoft.Json.Linq;
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.Jenkins;
  using Soloplan.WhatsON.Jenkins.GUI;
  using Soloplan.WhatsON.Model;
  using ApiServer = Soloplan.WhatsON.GUI.Tests.ProjectDiscoveryTests.DiscoveryServer;

  [TestFixture]
  [SetUICulture("en-US")]
  public class JenkinsObservedFailureTests
  {
    [TestCase("unstable-build-tests", 4, "Example.Tests.Suite1.Case4")]
    [TestCase("failed-build-tests", 4, "Example.Tests.Suite1.Case4")]
    [TestCase("many-failed-tests", 13, "Example.Tests.Suite1.Case5")]
    [TestCase("missing-test-report", 1, null)]
    [TestCase("stage-error", 0, null)]
    [TestCase("nested-stage-error", 0, null)]
    [TestCase("unstable-stage", 0, null)]
    [TestCase("no-stages", 0, null)]
    public async Task AnonymizedObservedResponsesReachNotificationsAndTooltips(string fixtureName, int count, string? expected)
    {
      using var fixtureStream = typeof(JenkinsObservedFailureTests).Assembly.GetManifestResourceStream(
        $"Soloplan.WhatsON.GUI.Tests.Fixtures.JenkinsFailures.{fixtureName}.json") ?? throw new AssertionException("Missing fixture.");
      using var reader = new StreamReader(fixtureStream);
      var fixture = JObject.Parse(await reader.ReadToEndAsync());
      await using var server = new ApiServer();
      server.Respond = path =>
      {
        if (path.Contains("/testReport/", StringComparison.Ordinal))
        {
          return ("200 OK", "application/xml", (string?)fixture["testReport"] ?? "<failures/>");
        }

        if (path.Contains("/execution/node/", StringComparison.Ordinal))
        {
          return ("200 OK", "application/json", fixture["nodes"]?[path.Split('/')[6]]?.ToString(Formatting.None) ?? "{}");
        }

        if (path.EndsWith("/wfapi/describe", StringComparison.Ordinal))
        {
          return ("200 OK", "application/json", fixture["pipeline"]?.ToString(Formatting.None) ?? "{}");
        }

        return ("200 OK", "application/json", new JObject { ["lastBuild"] = fixture["build"]?.DeepClone(), ["builds"] = new JArray() }.ToString(Formatting.None));
      };
      var connector = new JenkinsConnector(
        new ConnectorConfiguration("Jenkins", "Release", Connector.ServerAddress, server.Address, Connector.ProjectName, "Release"), new JenkinsApi());
      await connector.QueryStatus(CancellationToken.None);
      await connector.QueryStatus(CancellationToken.None);
      var status = connector.CurrentStatus ?? throw new AssertionException("Missing build status.");
      Assert.That(status.FailedTestCount, Is.EqualTo(count));
      Assert.That(status.State.ToString(), Is.EqualTo((string?)fixture["build"]?["result"]).IgnoreCase);
      Assert.That(server.Requests.Any(path => path.Contains("/testReport/", StringComparison.Ordinal) || path.EndsWith("/consoleText", StringComparison.Ordinal)), Is.False);
      var model = new JenkinsStatusViewModel(new ConnectorViewModel(connector) { Name = "Release" });
      model.Update(status);
      await model.LoadFailureDetails(CancellationToken.None);
      await model.LoadFailureDetails(CancellationToken.None);
      if (expected == null)
      {
        Assert.That(model.FailureDetails, Is.Null.Or.Empty, "Stage exit codes must not invent a failure cause.");
      }
      else
      {
        Assert.That(model.FailureSummary, Does.Contain(expected));
        Assert.That(TrayHandler.GetNotificationDescription(model), Does.Contain(expected));
      }

      if (fixtureName.Contains("build-tests", StringComparison.Ordinal) || fixtureName == "many-failed-tests")
      {
        Assert.That(server.Requests.Count(path => path.Contains("/testReport/", StringComparison.Ordinal)), Is.EqualTo(1), "Cache successful test reports.");
        Assert.That(server.Requests.Any(path => path.Contains("/wfapi/", StringComparison.Ordinal)), Is.False, "Test failures already explain these builds.");
      }

      Assert.That(server.Requests.Count(path => path.Contains("/execution/node/", StringComparison.Ordinal)), Is.LessThanOrEqualTo(1));
    }
  }
}
