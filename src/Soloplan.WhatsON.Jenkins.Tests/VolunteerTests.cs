// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VolunteerTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.Tests
{
  using System;
  using System.IO;
  using System.Linq;
  using System.Net.Http;
  using System.Text;
  using System.Threading;
  using System.Threading.Tasks;
  using Newtonsoft.Json;
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Jenkins.Model;
  using Soloplan.WhatsON.Model;
  using WireMock.RequestBuilders;
  using WireMock.ResponseBuilders;
  using WireMock.Server;

  [TestFixture]
  public class VolunteerTests
  {
    private string directory = string.Empty;
    private SecretProtector secrets = new SecretProtector(string.Empty);

    [SetUp]
    public void SetUp()
    {
      this.directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "volunteer-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(this.directory);
      this.secrets = new SecretProtector(Path.Combine(this.directory, "key"));
    }

    [TearDown]
    public void TearDown() => Directory.Delete(this.directory, true);

    [TestCase(null)]
    [TestCase("")]
    [TestCase("Existing description")]
    [TestCase("<p>Release notes</p>\r\nSecond line\r\n")]
    public void ClaimAndClearPreserveExistingDescription(string? description)
    {
      var claimed = JenkinsVolunteer.SetOwner(description, "Alex & team");
      Assert.That(JenkinsVolunteer.GetOwner(claimed), Is.EqualTo("Alex & team"));
      Assert.That(claimed, Does.Contain("Alex &amp; team"));
      Assert.That(JenkinsVolunteer.SetOwner(claimed, null), Is.EqualTo(description ?? string.Empty));
      Assert.That(JenkinsVolunteer.GetOwner(JenkinsVolunteer.SetOwner(claimed, null)), Is.Null);
    }

    [Test]
    public void AmbiguousMarkersAndInjectedLinesAreRejected()
    {
      var marker = JenkinsVolunteer.SetOwner(null, "alex");
      Assert.Throws<InvalidOperationException>(() => JenkinsVolunteer.SetOwner(marker + marker, null));
      Assert.Throws<ArgumentException>(() => JenkinsVolunteer.SetOwner(null, "alex\nother"));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task ApiAuthenticatesPreservesDescriptionAndVerifiesTheSavedMarker(bool claim)
    {
      using var server = WireMockServer.Start();
      var api = new JenkinsApi(this.secrets);
      var connector = this.CreateConnector(server.Urls[0], api);
      var original = claim ? "Release notes\r\n" : JenkinsVolunteer.SetOwner("Release notes\r\n", "alex");
      var updated = JenkinsVolunteer.SetOwner(original, claim ? "alex" : null);
      var path = "/job/folder/job/project/42";
      var auth = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("alex:fake-api-token"));
      server.Given(Request.Create().WithPath(path + "/api/json").WithHeader("Authorization", auth).UsingGet())
        .InScenario("claim")
        .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
          .WithBody(JsonConvert.SerializeObject(new JenkinsBuild { Number = 42, Result = "FAILURE", Description = original })));
      server.Given(Request.Create().WithPath(path + "/submitDescription").WithHeader("Authorization", auth).UsingPost())
        .InScenario("claim").WillSetStateTo("saved")
        .RespondWith(Response.Create().WithStatusCode(302).WithHeader("Location", "."));
      server.Given(Request.Create().WithPath(path + "/api/json").WithHeader("Authorization", auth).UsingGet())
        .InScenario("claim").WhenStateIs("saved")
        .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
          .WithBody(JsonConvert.SerializeObject(new JenkinsBuild { Number = 42, Result = "FAILURE", Description = updated })));

      var result = await api.SetVolunteer(connector, 42, claim ? null : "alex", claim, CancellationToken.None);
      Assert.That(result.Description, Is.EqualTo(updated));
      var post = server.LogEntries.Single(entry => entry.RequestMessage?.Method == "POST");
      Assert.That(Uri.UnescapeDataString((post.RequestMessage?.Body ?? string.Empty).Replace("+", " ")), Is.EqualTo("description=" + updated));
      Assert.That(server.LogEntries, Has.Count.EqualTo(3));
    }

    [Test]
    public void AnExistingOrChangedClaimIsNotOverwritten()
    {
      using var server = WireMockServer.Start();
      var api = new JenkinsApi(this.secrets);
      var connector = this.CreateConnector(server.Urls[0], api);
      server.Given(Request.Create().WithPath("/job/folder/job/project/42/api/json").UsingGet())
        .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
          .WithBody(JsonConvert.SerializeObject(new JenkinsBuild { Number = 42, Result = "FAILURE", Description = JenkinsVolunteer.SetOwner(null, "someone else") })));

      Assert.ThrowsAsync<InvalidOperationException>(() => api.SetVolunteer(connector, 42, null, true, CancellationToken.None));
      Assert.ThrowsAsync<InvalidOperationException>(() => api.SetVolunteer(connector, 42, "alex", false, CancellationToken.None));
      Assert.That(server.LogEntries.All(entry => entry.RequestMessage?.Method == "GET"), Is.True);
    }

    [TestCase(401)]
    [TestCase(403)]
    [TestCase(500)]
    public void FailedReadsNeverWrite(int statusCode)
    {
      using var server = WireMockServer.Start();
      var api = new JenkinsApi(this.secrets);
      var connector = this.CreateConnector(server.Urls[0], api);
      server.Given(Request.Create().UsingGet()).RespondWith(Response.Create().WithStatusCode(statusCode));
      Assert.ThrowsAsync<HttpRequestException>(() => api.SetVolunteer(connector, 42, null, true, CancellationToken.None));
      Assert.That(server.LogEntries, Has.Count.EqualTo(1));
    }

    [Test]
    public void PollingSeparatesCredentialsAndIncludesDescriptionInCompactMode()
    {
      var api = new JenkinsApi(this.secrets);
      var first = this.CreateConnector("https://builds.example", api);
      var second = this.CreateConnector("https://builds.example", api);
      second.Configuration.GetConfigurationByKey(JenkinsConnector.UserName).Value = "other";
      var batches = JenkinsApi.PlanPollingRequests(new[] { first, second }).ToList();
      Assert.That(batches, Has.Count.EqualTo(2));
      Assert.That(Uri.UnescapeDataString(JenkinsApi.PollingRequest(new[] { first }, true).Query), Does.Contain("description"));
      Assert.That(Uri.UnescapeDataString(JenkinsApi.PollingRequest(new[] { first }, true).Query), Does.Contain("lastStableBuild[number]"));
    }

    [Test]
    public async Task AuthenticatedPollingReadsMarkersForCurrentAndHistoricalBuilds()
    {
      using var server = WireMockServer.Start();
      var api = new JenkinsApi(this.secrets);
      var connector = this.CreateConnector(server.Urls[0], api);
      var auth = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("alex:fake-api-token"));
      var description = JenkinsVolunteer.SetOwner(null, "alex");
      server.Given(Request.Create().WithPath("/job/folder/api/xml").WithHeader("Authorization", auth).UsingGet())
        .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/xml")
          .WithBody("<jobs><job><name>project</name><lastStableBuild><number>40</number></lastStableBuild><lastBuild><number>42</number><result>FAILURE</result><description>" + description
            + "</description></lastBuild><build><number>41</number><result>FAILURE</result><description>" + description + "</description></build></job></jobs>"));
      var polling = await api.GetPollingJobs(new[] { connector }, CancellationToken.None);
      Assert.That(polling.Jobs[connector]?.LastStableBuild?.Number, Is.EqualTo(40));
      var result = await connector.QueryStatusBatch(new[] { connector }, CancellationToken.None);
      Assert.That(result, Does.Contain(connector));
      Assert.That(JenkinsVolunteer.GetOwner(connector.CurrentStatus?.Details), Is.EqualTo("alex"));
      Assert.That(JenkinsVolunteer.GetOwner(connector.Snapshots.Single(snapshot => snapshot.Status.BuildNumber == 41).Status.Details), Is.EqualTo("alex"));
    }

    [TestCase(403)]
    [TestCase(500)]
    public void FailedWritesAreReported(int statusCode)
    {
      using var server = WireMockServer.Start();
      var api = new JenkinsApi(this.secrets);
      var connector = this.CreateConnector(server.Urls[0], api);
      server.Given(Request.Create().UsingGet())
        .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
          .WithBody("{\"number\":42,\"result\":\"FAILURE\",\"description\":\"Original\"}"));
      server.Given(Request.Create().UsingPost()).RespondWith(Response.Create().WithStatusCode(statusCode));
      Assert.ThrowsAsync<HttpRequestException>(() => api.SetVolunteer(connector, 42, null, true, CancellationToken.None));
      Assert.That(server.LogEntries, Has.Count.EqualTo(2));
    }

    [Test]
    public void LoginRedirectCannotTurnIntoASuccessfulClaim()
    {
      using var server = WireMockServer.Start();
      var api = new JenkinsApi(this.secrets);
      var connector = this.CreateConnector(server.Urls[0], api);
      server.Given(Request.Create().UsingGet())
        .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
          .WithBody("{\"number\":42,\"result\":\"FAILURE\",\"description\":\"Original\"}"));
      server.Given(Request.Create().UsingPost())
        .RespondWith(Response.Create().WithStatusCode(302).WithHeader("Location", "/login"));
      Assert.ThrowsAsync<InvalidOperationException>(() => api.SetVolunteer(connector, 42, null, true, CancellationToken.None));
      Assert.That(server.LogEntries.Any(entry => entry.RequestMessage?.Path == "/login"), Is.False);
    }

    [Test]
    public void AuthenticatedReadsNeverForwardCredentialsToARedirect()
    {
      using var server = WireMockServer.Start();
      using var target = WireMockServer.Start();
      var api = new JenkinsApi(this.secrets);
      var connector = this.CreateConnector(server.Urls[0], api);
      server.Given(Request.Create().UsingGet())
        .RespondWith(Response.Create().WithStatusCode(302).WithHeader("Location", target.Urls[0] + "/login"));
      Assert.ThrowsAsync<HttpRequestException>(() => api.SetVolunteer(connector, 42, null, true, CancellationToken.None));
      Assert.That(target.LogEntries, Is.Empty);
    }

    [Test]
    public void TokensAreNotSentOverUnencryptedRemoteConnections()
    {
      var api = new JenkinsApi(this.secrets);
      var connector = this.CreateConnector("http://builds.example", api);
      Assert.ThrowsAsync<InvalidOperationException>(() => api.SetVolunteer(connector, 42, null, true, CancellationToken.None));
    }

    private JenkinsConnector CreateConnector(string address, JenkinsApi api)
    {
      var configuration = new ConnectorConfiguration("Jenkins", "project", Connector.ServerAddress, address, Connector.ProjectName, "folder/project");
      configuration.GetConfigurationByKey(JenkinsConnector.UserName).Value = "alex";
      configuration.GetConfigurationByKey(JenkinsConnector.ApiToken).Value = this.secrets.Encrypt("fake-api-token");
      configuration.GetConfigurationByKey(JenkinsConnector.RedirectPlugin).Value = "true";
      return new JenkinsConnector(configuration, api);
    }
  }
}
