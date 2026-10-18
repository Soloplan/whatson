// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NotificationConfigurationTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Tests
{
  using Newtonsoft.Json;
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Model;

  [TestFixture]
  public class NotificationConfigurationTests
  {
    [Test]
    public void ProjectWithoutOverridesInheritsGlobalSettingsWithoutAddingConfigurationItems()
    {
      var application = new ApplicationConfiguration();
      var connector = new ConnectorConfiguration("Jenkins", "Release");

      Assert.That(application.GetNotificationConfiguration(connector), Is.SameAs(application.NotificationConfiguration));
      Assert.That(connector.ConfigurationItems, Is.Empty);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("{broken json")]
    [TestCase("null")]
    public void MissingOrCorruptNotificationOverridesFallBackToGlobalSettings(string value)
    {
      var application = new ApplicationConfiguration();
      application.NotificationConfiguration.FailureNotificationEnabled = false;
      var connector = new ConnectorConfiguration("Jenkins", "Release", Connector.NotificationsVisbility, value);

      Assert.That(application.GetNotificationConfiguration(connector), Is.SameAs(application.NotificationConfiguration));
      Assert.That(application.GetNotificationConfiguration(connector).FailureNotificationEnabled, Is.False);
    }

    [Test]
    public void GlobalSettingsWinWhileProjectInheritanceIsEnabled()
    {
      var application = new ApplicationConfiguration();
      application.NotificationConfiguration.FailureNotificationEnabled = false;
      var settings = new ConnectorNotificationConfiguration { UseGlobalNotificationSettings = true, FailureNotificationEnabled = true };
      var connector = new ConnectorConfiguration("Jenkins", "Release", Connector.NotificationsVisbility, JsonConvert.SerializeObject(settings));

      Assert.That(application.GetNotificationConfiguration(connector).FailureNotificationEnabled, Is.False);
    }

    [Test]
    public void ProjectOverridesAreIsolatedFromGlobalAndOtherProjectsSettings()
    {
      var application = new ApplicationConfiguration();
      var settings = new ConnectorNotificationConfiguration
      {
        UseGlobalNotificationSettings = false, FailureNotificationEnabled = false, OnlyIfChanged = true,
      };
      var connector = new ConnectorConfiguration("Jenkins", "Release", Connector.NotificationsVisbility, JsonConvert.SerializeObject(settings));
      var other = new ConnectorConfiguration("Jenkins", "Other");

      var effective = application.GetNotificationConfiguration(connector);

      Assert.Multiple(() =>
      {
        Assert.That(effective.FailureNotificationEnabled, Is.False);
        Assert.That(effective.OnlyIfChanged, Is.True);
        Assert.That(application.NotificationConfiguration.FailureNotificationEnabled, Is.True);
        Assert.That(application.GetNotificationConfiguration(other).FailureNotificationEnabled, Is.True);
      });
    }
  }
}
