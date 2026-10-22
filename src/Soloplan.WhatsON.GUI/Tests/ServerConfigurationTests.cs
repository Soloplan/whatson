// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ServerConfigurationTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Tests
{
  using System.Linq;
  using System.Threading;
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Configuration.ViewModel;
  using Soloplan.WhatsON.Jenkins;
  using Soloplan.WhatsON.Model;

  [TestFixture]
  [Apartment(ApartmentState.STA)]
  public class ServerConfigurationTests
  {
    [Test]
    public void EditingResettingAndExportingCredentialsKeepChangesIsolatedUntilApplied()
    {
      var source = new ApplicationConfiguration();
      var project = new ConnectorConfiguration("Jenkins", "first", Connector.ServerAddress, "https://ci.example.invalid/", Connector.ProjectName, "first");
      project.GetConfigurationByKey(JenkinsConnector.UserName).Value = "alex";
      project.GetConfigurationByKey(JenkinsConnector.ApiToken).Value = "encrypted-token";
      source.ConnectorsConfiguration.Add(project);
      var model = new ConfigViewModel();
      model.Load(source);
      Assert.That(model.ConfigurationIsModified, Is.False);
      model.Servers.Single().GetConfigurationByKey(JenkinsConnector.ApiToken).Value = "changed-token";

      Assert.That(model.ConfigurationIsModified, Is.True);
      Assert.That(source.ServerConfigurations.Single().GetConfigurationByKey(JenkinsConnector.ApiToken).Value, Is.EqualTo("encrypted-token"));
      var exported = new ApplicationConfiguration();
      model.ApplyToConfiguration(exported);
      Assert.That(exported.ServerConfigurations.Single().GetConfigurationByKey(JenkinsConnector.ApiToken).Value, Is.EqualTo("changed-token"));
      Assert.That(exported.ConnectorsConfiguration.Single().GetConfigurationByKey(JenkinsConnector.ApiToken).Value, Is.EqualTo("changed-token"));
      Assert.That(exported.ConnectorsConfiguration.Single().ConfigurationItems.All(item => item.Key != JenkinsConnector.ApiToken), Is.True);
      model.Load(source);
      Assert.That(model.Servers.Single().GetConfigurationByKey(JenkinsConnector.ApiToken).Value, Is.EqualTo("encrypted-token"));
      Assert.That(model.ConfigurationIsModified, Is.False);
    }

    [Test]
    public void ChangingServersDoesNotCopyConflictingLegacyCredentialsToTheNewServer()
    {
      var source = new ApplicationConfiguration();
      foreach (var name in new[] { "first", "second" })
      {
        var project = new ConnectorConfiguration("Jenkins", name, Connector.ServerAddress, "https://ci.example.invalid/", Connector.ProjectName, name);
        project.GetConfigurationByKey(JenkinsConnector.UserName).Value = name;
        project.GetConfigurationByKey(JenkinsConnector.ApiToken).Value = name + "-token";
        source.ConnectorsConfiguration.Add(project);
      }

      var model = new ConfigViewModel();
      model.Load(source);
      model.Connectors.Last().GetConfigurationByKey(Connector.ServerAddress).Value = "https://other.example.invalid/";
      var exported = new ApplicationConfiguration();
      model.ApplyToConfiguration(exported);

      Assert.That(new JenkinsConnector(exported.ConnectorsConfiguration.Last(), new JenkinsApi()).HasApiCredentials, Is.False);
      Assert.That(exported.ServerConfigurations, Has.Count.EqualTo(2));
    }

    [Test]
    public void NewProjectsInheritExistingCredentialsAndNewServersGetOneEditor()
    {
      var source = new ApplicationConfiguration();
      var model = new ConfigViewModel();
      model.Load(source);
      foreach (var address in new[] { "https://ci.example.invalid/", "https://ci.example.invalid", "https://other.example.invalid/" })
      {
        var project = new ConnectorViewModel();
        project.Load(new ConnectorConfiguration("Jenkins", "project", Connector.ServerAddress, address, Connector.ProjectName, "project"));
        model.Connectors.Add(project);
      }

      model.RefreshServers();
      Assert.That(model.Servers, Has.Count.EqualTo(2));
      model.Servers[0].GetConfigurationByKey(JenkinsConnector.UserName).Value = "alex";
      model.Servers[0].GetConfigurationByKey(JenkinsConnector.ApiToken).Value = "encrypted-token";
      model.RefreshServers();
      var applied = new ApplicationConfiguration();
      model.ApplyToConfiguration(applied);

      Assert.That(applied.ConnectorsConfiguration.Take(2).All(project => project.GetConfigurationByKey(JenkinsConnector.ApiToken).Value == "encrypted-token"), Is.True);
      Assert.That(applied.ConnectorsConfiguration.Last().GetConfigurationByKey(JenkinsConnector.ApiToken).Value, Is.Null.Or.Empty);
    }
  }
}
