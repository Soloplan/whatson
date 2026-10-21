// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ServerConfigurationTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.Tests
{
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using Newtonsoft.Json;
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Model;
  using WireMock.RequestBuilders;
  using WireMock.ResponseBuilders;
  using WireMock.Server;

  [TestFixture]
  public class ServerConfigurationTests
  {
    [Test]
    public void ProjectsShareCredentialsAndMigrateTheFirstConfiguredAccount()
    {
      var application = new ApplicationConfiguration();
      var first = Project("https://CI.example.invalid:443/jenkins/", "first");
      var second = Project("https://ci.example.invalid/jenkins", "second", "alex", "encrypted-token");
      application.ConnectorsConfiguration.Add(first);
      application.ConnectorsConfiguration.Add(second);

      application.InitializeServerConfigurations();

      Assert.That(application.ServerConfigurations, Has.Count.EqualTo(1));
      Assert.That(first.ServerConfiguration, Is.SameAs(second.ServerConfiguration));
      Assert.That(first.GetConfigurationByKey(JenkinsConnector.UserName).Value, Is.EqualTo("alex"));
      Assert.That(first.ConfigurationItems.Any(item => item.Key == JenkinsConnector.ApiToken), Is.False);
      Assert.That(second.ConfigurationItems.Any(item => item.Key == JenkinsConnector.ApiToken), Is.False);
      var connector = new JenkinsConnector(first, new JenkinsApi());
      Assert.That(connector.HasApiCredentials, Is.True);
      second.GetConfigurationByKey(JenkinsConnector.ApiToken).Value = "replacement-token";
      Assert.That(connector.EncryptedApiToken, Is.EqualTo("replacement-token"));
      Assert.That(connector.AuthenticationUser, Is.EqualTo("alex"));
    }

    [TestCase("https://other.example.invalid/jenkins")]
    [TestCase("http://ci.example.invalid/jenkins")]
    [TestCase("https://ci.example.invalid:8443/jenkins")]
    [TestCase("https://ci.example.invalid/other")]
    [TestCase("https://ci.example.invalid/Jenkins")]
    public void CredentialsAreIsolatedByServerIncludingSchemePortAndContextPath(string address)
    {
      var application = new ApplicationConfiguration();
      var first = Project("https://ci.example.invalid/jenkins", "first", "alex", "encrypted-token");
      var second = Project(address, "second");
      application.ConnectorsConfiguration.Add(first);
      application.ConnectorsConfiguration.Add(second);

      application.InitializeServerConfigurations();

      Assert.That(application.ServerConfigurations, Has.Count.EqualTo(2));
      Assert.That(new JenkinsConnector(second, new JenkinsApi()).HasApiCredentials, Is.False);
    }

    [Test]
    public void MigrationPrefersACompleteAccountOverAnEarlierPartialAccount()
    {
      var application = new ApplicationConfiguration();
      application.ConnectorsConfiguration.Add(Project("https://ci.example.invalid/", "first", "sam"));
      application.ConnectorsConfiguration.Add(Project("https://ci.example.invalid/", "second", "alex", "encrypted-token"));
      application.InitializeServerConfigurations();

      var connector = new JenkinsConnector(application.ConnectorsConfiguration[0], new JenkinsApi());
      Assert.That(connector.AuthenticationUser, Is.EqualTo("alex"));
      Assert.That(connector.EncryptedApiToken, Is.EqualTo("encrypted-token"));
    }

    [TestCase("alex", null)]
    [TestCase(null, "encrypted-token")]
    [TestCase(" ", "encrypted-token")]
    [TestCase("alex", " ")]
    public async Task IncompleteLegacyCredentialsDoNotDisableAnonymousPollingForOtherProjects(string? user, string? token)
    {
      using var server = WireMockServer.Start();
      server.Given(Request.Create().WithPath("/api/xml").UsingGet())
        .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/xml")
          .WithBody("<jobs><job><name>first</name></job><job><name>second</name></job></jobs>"));
      var application = new ApplicationConfiguration();
      var first = Project(server.Urls[0], "first");
      var second = Project(server.Urls[0], "second", user, token);
      application.ConnectorsConfiguration.Add(first);
      application.ConnectorsConfiguration.Add(second);
      application.InitializeServerConfigurations();
      application.InitializeServerConfigurations();
      var api = new JenkinsApi();
      var connectors = application.ConnectorsConfiguration.Select(project => new JenkinsConnector(project, api)).ToArray();
      var result = await api.GetPollingJobs(connectors, CancellationToken.None);

      Assert.That(result.Jobs.Keys, Is.EquivalentTo(connectors));
      Assert.That(result.Failed, Is.Empty);
      Assert.That(server.LogEntries, Has.Count.EqualTo(1));
      Assert.That(server.LogEntries.Single().RequestMessage?.Headers?.ContainsKey("Authorization"), Is.False);
      Assert.That(second.ConfigurationItems.FirstOrDefault(item => item.Key == JenkinsConnector.UserName)?.Value, Is.EqualTo(user));
      Assert.That(second.ConfigurationItems.FirstOrDefault(item => item.Key == JenkinsConnector.ApiToken)?.Value, Is.EqualTo(token));
    }

    [Test]
    public void SavedServerCredentialsRoundTripWithoutDuplicatingSecretsInProjects()
    {
      var application = new ApplicationConfiguration();
      application.ConnectorsConfiguration.Add(Project("https://ci.example.invalid/", "first", "alex", "encrypted-token"));
      application.ConnectorsConfiguration.Add(Project("https://ci.example.invalid/", "second"));
      application.InitializeServerConfigurations();
      var json = JsonConvert.SerializeObject(application);
      var restored = JsonConvert.DeserializeObject<ApplicationConfiguration>(json) ?? throw new AssertionException("Configuration is missing.");
      restored.InitializeServerConfigurations();
      restored.InitializeServerConfigurations();

      Assert.That(restored.ServerConfigurations, Has.Count.EqualTo(1));
      Assert.That(json.Split("encrypted-token"), Has.Length.EqualTo(2));
      Assert.That(restored.ConnectorsConfiguration.All(project => project.GetConfigurationByKey(JenkinsConnector.ApiToken).Value == "encrypted-token"), Is.True);
      Assert.That(restored.ConnectorsConfiguration.All(project => project.ConfigurationItems.All(item => item.Key != JenkinsConnector.ApiToken)), Is.True);
    }

    [TestCase("https://other.example.invalid/")]
    [TestCase("https://other.example.invalid/?query")]
    public void ConflictingLegacyCredentialsArePreservedAndCannotOverrideOrRestoreClearedServerCredentials(string address)
    {
      var application = new ApplicationConfiguration();
      var first = Project("https://ci.example.invalid/", "first", "alex", "first-token");
      var second = Project("https://ci.example.invalid/", "second", "sam", "second-token");
      application.ConnectorsConfiguration.Add(first);
      application.ConnectorsConfiguration.Add(second);
      application.InitializeServerConfigurations();

      Assert.That(second.GetConfigurationByKey(JenkinsConnector.ApiToken).Value, Is.EqualTo("first-token"));
      Assert.That(second.ConfigurationItems.Single(item => item.Key == JenkinsConnector.ApiToken).Value, Is.EqualTo("second-token"));
      var server = application.ServerConfigurations.Single();
      server.GetConfigurationByKey(JenkinsConnector.UserName).Value = string.Empty;
      server.GetConfigurationByKey(JenkinsConnector.ApiToken).Value = string.Empty;
      application.InitializeServerConfigurations();

      Assert.That(new JenkinsConnector(second, new JenkinsApi()).HasApiCredentials, Is.False);
      Assert.That(application.ServerConfigurations, Has.Count.EqualTo(1));
      second.GetConfigurationByKey(Connector.ServerAddress).Value = address;
      application.InitializeServerConfigurations();
      Assert.That(new JenkinsConnector(second, new JenkinsApi()).HasApiCredentials, Is.False, "Changing servers must not activate retained legacy credentials.");
    }

    [Test]
    public void ChangingTheServerAddressRebindsCredentialsAndLeavesCruiseControlAlone()
    {
      var application = new ApplicationConfiguration();
      var first = Project("https://ci.example.invalid/", "first", "alex", "encrypted-token");
      var cruiseControl = new ConnectorConfiguration("CruiseControl", "other", Connector.ServerAddress, "https://ci.example.invalid/");
      application.ConnectorsConfiguration.Add(first);
      application.ConnectorsConfiguration.Add(cruiseControl);
      application.InitializeServerConfigurations();
      first.GetConfigurationByKey(Connector.ServerAddress).Value = "https://other.example.invalid/";
      application.InitializeServerConfigurations();

      Assert.That(first.GetConfigurationByKey(JenkinsConnector.ApiToken).Value, Is.Null.Or.Empty);
      Assert.That(cruiseControl.ServerConfiguration, Is.Null);
    }

    private static ConnectorConfiguration Project(string address, string name, string? user = null, string? token = null)
    {
      var project = new ConnectorConfiguration("Jenkins", name, Connector.ServerAddress, address, Connector.ProjectName, name);
      if (user != null || token != null)
      {
        project.GetConfigurationByKey(JenkinsConnector.UserName).Value = user;
        project.GetConfigurationByKey(JenkinsConnector.ApiToken).Value = token;
      }

      return project;
    }
  }
}
