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
  using System.Xml.Linq;
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
    [TestCase("Existing description\n")]
    [TestCase("Existing description\r\n\r\n")]
    [TestCase("<p>Release notes</p>\r\nSecond line\r\n")]
    [TestCase("<hr /><p>Release notes</p><hr />")]
    public void ClaimAndClearPreserveExistingDescription(string? description)
    {
      var claimed = JenkinsVolunteer.SetOwner(description, "Alex & team");
      Assert.That(JenkinsVolunteer.GetOwner(claimed), Is.EqualTo("Alex & team"));
      Assert.That(claimed, Does.Contain("ALEX &amp; TEAM"));
      Assert.That(claimed, Is.EqualTo((description ?? string.Empty) + JenkinsVolunteer.SetOwner(null, "Alex & team")));
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

    [TestCase("Alex & <team> [on call]")]
    [TestCase("Jörg 中文 $1")]
    [TestCase("--><!-- whatsON:volunteer:end -->")]
    public void CommentMarkersRoundTripNamesWithoutAllowingMarkupInjection(string owner)
    {
      var marker = JenkinsVolunteer.SetOwner(null, owner);
      Assert.That(marker, Does.StartWith("<!-- whatsON:volunteer:start -->"));
      Assert.That(marker, Does.EndWith("<!-- whatsON:volunteer:end -->"));
      Assert.That(marker, Does.Contain("<strong>" + System.Net.WebUtility.HtmlEncode(owner.ToUpperInvariant()) + "</strong>"));
      Assert.That(JenkinsVolunteer.GetOwner(marker), Is.EqualTo(owner));
      Assert.That(JenkinsVolunteer.SetOwner(marker, null), Is.Empty);
    }

    [TestCase("\n")]
    [TestCase("\r\n")]
    public void ExtraLineBreaksInsideTheNoticeAreSupported(string newline)
    {
      var marker = JenkinsVolunteer.SetOwner(null, "alex");
      var description = "Before" + marker.Replace("\n", newline + newline) + "After";
      Assert.That(JenkinsVolunteer.GetOwner(description), Is.EqualTo("alex"));
      Assert.That(JenkinsVolunteer.SetOwner(description, null), Is.EqualTo("BeforeAfter"));
      Assert.That(JenkinsVolunteer.SetOwner(description, "other"), Is.EqualTo("BeforeAfter" + JenkinsVolunteer.SetOwner(null, "other")));
    }

    [Test]
    public void ReplacementUsesBoundariesAndPreservesSurroundingText()
    {
      var marker = JenkinsVolunteer.SetOwner(null, "alex").Replace("Volunteer: <strong>ALEX</strong>", "<strong>Edited notice</strong>\r\nAnother line");
      var description = "<p>Before</p>\r\n" + marker + "\r\n<!-- unrelated -->After";
      Assert.That(JenkinsVolunteer.GetOwner(description), Is.EqualTo("alex"));
      Assert.That(JenkinsVolunteer.SetOwner(description, "other"),
        Is.EqualTo("<p>Before</p>\r\n\r\n<!-- unrelated -->After" + JenkinsVolunteer.SetOwner(null, "other")));
      Assert.That(JenkinsVolunteer.SetOwner(description, null), Is.EqualTo("<p>Before</p>\r\n\r\n<!-- unrelated -->After"));
    }

    [Test]
    public void LegacyMarkersCanStillBeReadClearedAndReplaced()
    {
      const string Description = "Before\r\n[whatsON] Being fixed by Alex &amp; team [/whatsON]\r\nAfter";
      Assert.That(JenkinsVolunteer.GetOwner(Description), Is.EqualTo("Alex & team"));
      Assert.That(JenkinsVolunteer.SetOwner(Description, null), Is.EqualTo("Before\r\nAfter"));
      Assert.That(JenkinsVolunteer.SetOwner(Description, "other"), Is.EqualTo("Before\r\nAfter" + JenkinsVolunteer.SetOwner(null, "other")));
      Assert.Throws<InvalidOperationException>(() => JenkinsVolunteer.SetOwner(Description + JenkinsVolunteer.SetOwner(null, "other"), null));
    }

    [Test]
    public void CompactBannerEmphasizesTheUppercaseOwnerWithoutOverridingThemeColors()
    {
      const string Owner = "Alex & <team>";
      var marker = JenkinsVolunteer.SetOwner(null, Owner);
      var markup = XElement.Parse("<description>" + marker + "</description>");
      Assert.That(markup.Elements().Select(element => element.Name.LocalName), Is.EqualTo(new[] { "hr", "div", "hr" }));
      var banner = markup.Elements("div").Single();
      Assert.That(banner.Elements("h3").Single().Value, Is.EqualTo("Fix in progress"));
      Assert.That(banner.Elements("p").Single().Value, Is.EqualTo("Volunteer: " + Owner.ToUpperInvariant()));
      Assert.That(banner.Descendants("strong").Single().Value, Is.EqualTo(Owner.ToUpperInvariant()));
      Assert.That(banner.Attribute("style")?.Value, Does.Contain("border-left: 3px solid #3b82f6"));
      Assert.That(banner.Attribute("style")?.Value, Does.Contain("padding: 6px 10px"));
      Assert.That(banner.Elements("h3").Single().Attribute("style")?.Value, Does.Contain("font-size: 14px"));
      Assert.That(banner.Elements("p").Single().Attribute("style")?.Value, Does.Contain("font-size: 12px"));
      Assert.That(JenkinsVolunteer.GetOwner(marker), Is.EqualTo(Owner), "Display casing must not change the account identity.");
      foreach (var style in markup.Descendants().Attributes("style"))
      {
        Assert.That(style.Value, Does.Not.Match(@"(^|;)\s*(color|background(?:-color)?)\s*:"), "The notice must inherit Jenkins theme colors.");
      }

      Assert.That(JenkinsVolunteer.SetOwner(marker, null), Is.Empty, "Clearing must remove both rules and the entire banner.");
    }

    [Test]
    public void PreviousCommentNoticesCanStillBeReadClearedAndReplaced()
    {
      const string Marker = "<!-- whatsON:volunteer:start -->\n<!-- whatsON:volunteer:owner YWxleA== -->\n<br />\nBeing fixed by alex\n<!-- whatsON:volunteer:end -->";
      var description = "<p>Before</p>" + Marker + "<hr />After";
      Assert.That(JenkinsVolunteer.GetOwner(description), Is.EqualTo("alex"));
      Assert.That(JenkinsVolunteer.SetOwner(description, null), Is.EqualTo("<p>Before</p><hr />After"));
      Assert.That(JenkinsVolunteer.SetOwner(description, "other"), Is.EqualTo("<p>Before</p><hr />After" + JenkinsVolunteer.SetOwner(null, "other")));
    }

    [TestCase("<!-- whatsON:volunteer:start -->Keep this text")]
    [TestCase("Keep this text<!-- whatsON:volunteer:end -->")]
    [TestCase("<!-- whatsON:volunteer:end --><!-- whatsON:volunteer:start -->")]
    [TestCase("<!-- whatsON:volunteer:start -->Missing owner<!-- whatsON:volunteer:end -->")]
    public void IncompleteCommentMarkersPreventEdits(string description)
    {
      Assert.Throws<InvalidOperationException>(() => JenkinsVolunteer.SetOwner(description, null));
      Assert.Throws<InvalidOperationException>(() => JenkinsVolunteer.SetOwner(description, "alex"));
    }

    [TestCase("A")]
    [TestCase("/w==")]
    public void CorruptedOwnerMetadataDoesNotBreakStatusReading(string encodedOwner)
    {
      var marker = "<!-- whatsON:volunteer:start --><!-- whatsON:volunteer:owner " + encodedOwner
        + " -->Notice<!-- whatsON:volunteer:end -->";
      Assert.That(JenkinsVolunteer.GetOwner(marker), Is.Null);
    }

    [TestCase(true, "alex")]
    [TestCase(false, "alex")]
    [TestCase(false, "sam")]
    public async Task ApiAuthenticatesPreservesDescriptionAndVerifiesTheSavedMarker(bool claim, string userName)
    {
      using var server = WireMockServer.Start();
      var api = new JenkinsApi(this.secrets);
      var connector = this.CreateConnector(server.Urls[0], api);
      connector.Configuration.GetConfigurationByKey(JenkinsConnector.UserName).Value = userName;
      var original = claim ? "Release notes\r\n" : JenkinsVolunteer.SetOwner("Release notes\r\n", "alex");
      var updated = JenkinsVolunteer.SetOwner(original, claim ? userName : null);
      var path = "/job/folder/job/project/42";
      var auth = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(userName + ":fake-api-token"));
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

    [TestCase(true)]
    [TestCase(false)]
    public async Task JobPanelUpdatesAuthenticatePreserveContextAndVerifyTheSavedDescription(bool claim)
    {
      using var server = WireMockServer.Start();
      var api = new JenkinsApi(this.secrets);
      var connector = this.CreateConnector(server.Urls[0] + "/jenkins", api);
      var original = JenkinsVolunteerPanel.SetFix("<p>Job documentation</p>", 34, null, "sam");
      if (!claim)
      {
        original = JenkinsVolunteerPanel.SetFix(original, 42, null, "alex");
      }

      var updated = JenkinsVolunteerPanel.SetFix(original, 42, claim ? null : "alex", claim ? "alex" : null);
      var path = "/jenkins/job/folder/job/project";
      var auth = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("alex:fake-api-token"));
      server.Given(Request.Create().WithPath(path + "/api/json").WithHeader("Authorization", auth).UsingGet())
        .InScenario("panel")
        .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
          .WithBody(JsonConvert.SerializeObject(new JenkinsJob { Description = original })));
      server.Given(Request.Create().WithPath(path + "/submitDescription").WithHeader("Authorization", auth).UsingPost())
        .InScenario("panel").WillSetStateTo("saved")
        .RespondWith(Response.Create().WithStatusCode(302).WithHeader("Location", "."));
      server.Given(Request.Create().WithPath(path + "/api/json").WithHeader("Authorization", auth).UsingGet())
        .InScenario("panel").WhenStateIs("saved")
        .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
          .WithBody(JsonConvert.SerializeObject(new JenkinsJob { Description = updated })));

      Assert.That(await api.SetJobVolunteer(connector, 42, claim ? null : "alex", claim ? "alex" : null, CancellationToken.None), Is.EqualTo(updated));
      var post = server.LogEntries.Single(entry => entry.RequestMessage?.Method == "POST");
      Assert.That(Uri.UnescapeDataString((post.RequestMessage?.Body ?? string.Empty).Replace("+", " ")), Is.EqualTo("description=" + updated));
      Assert.That(server.LogEntries, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task ADeletedBuildDoesNotPreventRemovingItsJobAssignment()
    {
      using var server = WireMockServer.Start();
      var api = new JenkinsApi(this.secrets);
      var connector = this.CreateConnector(server.Urls[0], api);
      server.Given(Request.Create().UsingGet()).RespondWith(Response.Create().WithStatusCode(404));

      var cleared = await api.SetVolunteer(connector, 42, "alex", false, CancellationToken.None);

      Assert.That(cleared.Number, Is.EqualTo(42));
      Assert.That(cleared.Description, Is.Null);
      Assert.ThrowsAsync<HttpRequestException>(() => api.SetVolunteer(connector, 42, null, true, CancellationToken.None));
      Assert.That(server.LogEntries.All(entry => entry.RequestMessage?.Method == "GET"), Is.True);
    }

    [Test]
    public async Task AnAlreadyRemovedBuildNoticeDoesNotPreventRemovingItsJobAssignment()
    {
      using var server = WireMockServer.Start();
      var api = new JenkinsApi(this.secrets);
      var connector = this.CreateConnector(server.Urls[0], api);
      server.Given(Request.Create().UsingGet())
        .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
          .WithBody("{\"number\":42,\"result\":\"FAILURE\",\"description\":\"Release notes\"}"));

      var cleared = await api.SetVolunteer(connector, 42, "alex", false, CancellationToken.None);

      Assert.That(cleared.Description, Is.EqualTo("Release notes"));
      Assert.That(server.LogEntries, Has.Count.EqualTo(1));
    }

    [Test]
    public void AnUnexpectedJobResponseNeverOverwritesTheDescription()
    {
      using var server = WireMockServer.Start();
      var api = new JenkinsApi(this.secrets);
      var connector = this.CreateConnector(server.Urls[0], api);
      server.Given(Request.Create().UsingGet())
        .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody("{}"));

      Assert.ThrowsAsync<InvalidPlugInApiResponseException>(() => api.SetJobVolunteer(connector, 42, null, "alex", CancellationToken.None));

      Assert.That(server.LogEntries, Has.Count.EqualTo(1));
    }

    [Test]
    public void JobPanelPermissionFailuresExplainTheRequiredPermission()
    {
      using var server = WireMockServer.Start();
      var api = new JenkinsApi(this.secrets);
      var connector = this.CreateConnector(server.Urls[0], api);
      server.Given(Request.Create().UsingGet())
        .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody("{\"description\":\"Notes\"}"));
      server.Given(Request.Create().UsingPost()).RespondWith(Response.Create().WithStatusCode(403));
      var error = Assert.ThrowsAsync<HttpRequestException>(() => api.SetJobVolunteer(connector, 42, null, "alex", CancellationToken.None));
      Assert.That(error?.Message, Does.Contain("Job/Configure"));
    }

    [Test]
    public void JobPanelLoginRedirectsAreNotFollowedOrTreatedAsSuccess()
    {
      using var server = WireMockServer.Start();
      var api = new JenkinsApi(this.secrets);
      var connector = this.CreateConnector(server.Urls[0], api);
      server.Given(Request.Create().UsingGet())
        .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody("{\"description\":\"Notes\"}"));
      server.Given(Request.Create().UsingPost()).RespondWith(Response.Create().WithStatusCode(302).WithHeader("Location", "/login"));
      Assert.ThrowsAsync<InvalidOperationException>(() => api.SetJobVolunteer(connector, 42, null, "alex", CancellationToken.None));
      Assert.That(server.LogEntries.Any(entry => entry.RequestMessage?.Path == "/login"), Is.False);
    }

    [Test]
    public async Task CredentialTestAuthenticatesWithOneReadOnlyRequestAndPreservesContextPath()
    {
      using var server = WireMockServer.Start();
      var api = new JenkinsApi(this.secrets);
      var connector = this.CreateConnector(server.Urls[0] + "/jenkins", api);
      var ciphertext = connector.EncryptedApiToken;
      var auth = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("alex:fake-api-token"));
      server.Given(Request.Create().WithPath("/jenkins/whoAmI/api/json").WithHeader("Authorization", auth).UsingGet())
        .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
          .WithBody("{\"authenticated\":true,\"anonymous\":false,\"name\":\"alex\"}"));

      Assert.That(await api.TestAuthentication(connector, CancellationToken.None), Is.EqualTo("alex"));
      Assert.That(connector.EncryptedApiToken, Is.EqualTo(ciphertext));
      Assert.That(server.LogEntries, Has.Count.EqualTo(1));
      Assert.That(server.LogEntries.Single().RequestMessage?.Method, Is.EqualTo("GET"));
    }

    [TestCase("{\"authenticated\":true,\"anonymous\":true,\"name\":\"anonymous\"}")]
    [TestCase("{\"authenticated\":false,\"anonymous\":false,\"name\":\"alex\"}")]
    [TestCase("{\"authenticated\":true,\"anonymous\":false,\"name\":\"\"}")]
    public void CredentialTestRejectsUnauthenticatedIdentities(string identity)
    {
      using var server = WireMockServer.Start();
      var api = new JenkinsApi(this.secrets);
      var connector = this.CreateConnector(server.Urls[0], api);
      server.Given(Request.Create().WithPath("/whoAmI/api/json").UsingGet())
        .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody(identity));
      Assert.ThrowsAsync<HttpRequestException>(() => api.TestAuthentication(connector, CancellationToken.None));
    }

    [TestCase("application/json", "{}")]
    [TestCase("application/json", "{\"authenticated\":\"true\",\"anonymous\":false,\"name\":\"alex\"}")]
    [TestCase("text/html", "<html>Sign in</html>")]
    public void CredentialTestRejectsUnexpectedResponses(string contentType, string body)
    {
      using var server = WireMockServer.Start();
      var api = new JenkinsApi(this.secrets);
      var connector = this.CreateConnector(server.Urls[0], api);
      server.Given(Request.Create().WithPath("/whoAmI/api/json").UsingGet())
        .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", contentType).WithBody(body));
      Assert.ThrowsAsync<InvalidPlugInApiResponseException>(() => api.TestAuthentication(connector, CancellationToken.None));
    }

    [TestCase(401)]
    [TestCase(403)]
    [TestCase(302)]
    public void CredentialTestRejectsAccessFailuresAndDoesNotFollowRedirects(int status)
    {
      using var server = WireMockServer.Start();
      var api = new JenkinsApi(this.secrets);
      var connector = this.CreateConnector(server.Urls[0], api);
      server.Given(Request.Create().WithPath("/whoAmI/api/json").UsingGet())
        .RespondWith(Response.Create().WithStatusCode(status).WithHeader("Location", server.Urls[0] + "/other"));
      Assert.ThrowsAsync<HttpRequestException>(() => api.TestAuthentication(connector, CancellationToken.None));
      Assert.That(server.LogEntries, Has.Count.EqualTo(1));
    }

    [TestCase(Connector.ServerAddress)]
    [TestCase(JenkinsConnector.UserName)]
    [TestCase(JenkinsConnector.ApiToken)]
    public void CredentialTestRequiresAllSettingsBeforeSendingARequest(string missingKey)
    {
      using var server = WireMockServer.Start();
      var api = new JenkinsApi(this.secrets);
      var connector = this.CreateConnector(server.Urls[0], api);
      connector.Configuration.GetConfigurationByKey(missingKey).Value = string.Empty;
      Assert.ThrowsAsync<InvalidOperationException>(() => api.TestAuthentication(connector, CancellationToken.None));
      Assert.That(server.LogEntries, Is.Empty);
    }

    [TestCase("http://ci.example.invalid")]
    [TestCase("ftp://localhost")]
    [TestCase("https://user:password@ci.example.invalid")]
    public void CredentialTestRejectsUnsafeAddresses(string address)
    {
      var api = new JenkinsApi(this.secrets);
      var connector = this.CreateConnector(address, api);
      Assert.ThrowsAsync<InvalidOperationException>(() => api.TestAuthentication(connector, CancellationToken.None));
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

    [TestCase("damaged")]
    [TestCase("duplicate")]
    [TestCase("incomplete")]
    public void CorruptedNoticeNeverTriggersAnApiWrite(string damage)
    {
      using var server = WireMockServer.Start();
      var api = new JenkinsApi(this.secrets);
      var connector = this.CreateConnector(server.Urls[0], api);
      var marker = JenkinsVolunteer.SetOwner(null, "alex");
      var description = damage switch
      {
        "duplicate" => marker + marker,
        "incomplete" => marker.Replace("<!-- whatsON:volunteer:end -->", string.Empty),
        _ => marker.Replace("YWxleA==", "/w=="),
      };
      server.Given(Request.Create().WithPath("/job/folder/job/project/42/api/json").UsingGet())
        .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
          .WithBody(JsonConvert.SerializeObject(new JenkinsBuild { Number = 42, Result = "FAILURE", Description = description })));
      Assert.ThrowsAsync<InvalidOperationException>(() => api.SetVolunteer(connector, 42, null, true, CancellationToken.None));
      Assert.ThrowsAsync<InvalidOperationException>(() => api.SetVolunteer(connector, 42, null, false, CancellationToken.None));
      Assert.That(server.LogEntries.All(entry => entry.RequestMessage?.Method == "GET"), Is.True);
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
      var description = System.Net.WebUtility.HtmlEncode(JenkinsVolunteer.SetOwner(null, "alex"));
      var panel = System.Net.WebUtility.HtmlEncode(JenkinsVolunteerPanel.SetFix("Job documentation", 41, null, "sam"));
      server.Given(Request.Create().WithPath("/job/folder/api/xml").WithHeader("Authorization", auth).UsingGet())
        .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/xml")
          .WithBody("<jobs><job><name>project</name><description>" + panel + "</description><lastStableBuild><number>40</number></lastStableBuild><lastBuild><number>42</number><result>FAILURE</result><description>" + description
            + "</description></lastBuild><build><number>41</number><result>FAILURE</result><description>" + description + "</description></build></job></jobs>"));
      var polling = await api.GetPollingJobs(new[] { connector }, CancellationToken.None);
      Assert.That(polling.Jobs[connector]?.LastStableBuild?.Number, Is.EqualTo(40));
      var result = await connector.QueryStatusBatch(new[] { connector }, CancellationToken.None);
      Assert.That(result, Does.Contain(connector));
      Assert.That(JenkinsVolunteer.GetOwner(connector.CurrentStatus?.Details), Is.EqualTo("alex"));
      Assert.That((connector.CurrentStatus as JenkinsStatus)?.ActiveFixes.Single(), Is.EqualTo(new JenkinsFix(41, "sam")));
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
