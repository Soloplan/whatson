// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsPluginTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.Tests
{
  using System.Collections.Generic;
  using Newtonsoft.Json;
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Model;
  using WireMock.Server;

  [TestFixture]
  public class JenkinsPluginTests
  {
    [TestCase("True")]
    [TestCase("False")]
    public void ImportedProjectsKeepTheirRedirectChoiceWithoutProbingTheServer(string redirect)
    {
      using var server = WireMockServer.Start();
      var address = server.Urls[0] + "/";
      var plugin = new JenkinsPlugin();
      foreach (var name in new[] { "release/net", "release/installer", "release/image" })
      {
        var configuration = new ConnectorConfiguration(
          "Jenkins",
          name,
          [
            new ConfigurationItem(Connector.ProjectName, $"product-releases/{name}"),
            new ConfigurationItem(Connector.ServerAddress, address),
            new ConfigurationItem(JenkinsConnector.RedirectPlugin, redirect),
            new ConfigurationItem(Connector.Category, "Product release"),
          ]);
        var saved = JsonConvert.SerializeObject(configuration);
        var imported = JsonConvert.DeserializeObject<ConnectorConfiguration>(saved) ?? throw new AssertionException("Deserialization returned null.");

        var connector = plugin.CreateNew(imported, true);

        Assert.That(connector, Is.InstanceOf<JenkinsConnector>());
        Assert.That(connector.Configuration, Is.SameAs(imported));
        Assert.That(connector.Project, Is.EqualTo($"product-releases/{name}"));
        Assert.That(JsonConvert.SerializeObject(imported), Is.EqualTo(saved), "Import must preserve the saved settings.");
        Assert.That(server.LogEntries, Is.Empty, "A saved redirect choice must not trigger network detection.");
      }
    }
  }
}
