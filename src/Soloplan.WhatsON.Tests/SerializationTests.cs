// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SerializationTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Tests
{
  using System;
  using System.IO;
  using System.Linq;
  using Newtonsoft.Json;
  using Newtonsoft.Json.Linq;
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Model;

  [TestFixture]
  public class SerializationTests
  {
    private readonly string directory = Path.Combine(Path.GetTempPath(), "whatson-tests-" + Guid.NewGuid());

    private readonly SerializationHelper serializer = new SerializationHelper();

    [SetUp]
    public void SetUp()
    {
      this.serializer.SetConfigFolder(this.directory);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(this.directory, true);

    [Test]
    public void SaveAndLoadPreserveProjectsIdentitiesAndNotificationPreferences()
    {
      var configuration = new ApplicationConfiguration { ThemeMode = ApplicationTheme.Dark, AlwaysOnTop = true, OpenMinimized = true };
      configuration.NotificationConfiguration.OnlyIfChanged = true;
      configuration.NotificationConfiguration.SuccessNotificationEnabled = false;
      var connector = new ConnectorConfiguration(
        "Jenkins",
        "Release – 日本語",
        Connector.ServerAddress,
        "https://builds.example/",
        Connector.ProjectName,
        "folder/release");
      var overrideSettings = new ConnectorNotificationConfiguration { UseGlobalNotificationSettings = false, FailureNotificationEnabled = false };
      connector.ConfigurationItems.Add(new ConfigurationItem(Connector.NotificationsVisbility, JsonConvert.SerializeObject(overrideSettings)));
      configuration.ConnectorsConfiguration.Add(connector);
      configuration.ConnectorsConfiguration.Add(new ConnectorConfiguration("Jenkins", "Other project"));

      this.serializer.SaveConfiguration(configuration);
      var restored = this.serializer.LoadConfiguration();

      Assert.Multiple(() =>
      {
        Assert.That(restored.ThemeMode, Is.EqualTo(ApplicationTheme.Dark));
        Assert.That(restored.AlwaysOnTop, Is.True);
        Assert.That(restored.OpenMinimized, Is.True);
        Assert.That(restored.NotificationConfiguration.OnlyIfChanged, Is.True);
        Assert.That(restored.NotificationConfiguration.SuccessNotificationEnabled, Is.False);
        Assert.That(restored.ConnectorsConfiguration.Select(c => c.Name), Is.EqualTo(configuration.ConnectorsConfiguration.Select(c => c.Name)));
        Assert.That(
          restored.ConnectorsConfiguration.Select(c => c.Identifier),
          Is.EqualTo(configuration.ConnectorsConfiguration.Select(c => c.Identifier)));
        Assert.That(restored.ConnectorsConfiguration[0].GetConfigurationByKey(Connector.ProjectName).Value, Is.EqualTo("folder/release"));
        Assert.That(restored.GetNotificationConfiguration(restored.ConnectorsConfiguration[0]).FailureNotificationEnabled, Is.False);
      });
    }

    [Test]
    public void ExportWithoutIdentifiersCreatesIndependentProjectsOnImport()
    {
      var configuration = new ApplicationConfiguration();
      configuration.ConnectorsConfiguration.Add(new ConnectorConfiguration("Jenkins", "First"));
      configuration.ConnectorsConfiguration.Add(new ConnectorConfiguration("Jenkins", "Second"));

      this.serializer.Save(configuration, this.serializer.ConfigFile, false);
      var exported = JObject.Parse(File.ReadAllText(this.serializer.ConfigFile));
      var imported = this.serializer.LoadConfiguration();

      Assert.Multiple(() =>
      {
        Assert.That((exported["ConnectorsConfiguration"] ?? throw new AssertionException("Missing connectors.")).Children().All(c => c["Identifier"] == null), Is.True);
        Assert.That(imported.ConnectorsConfiguration.Select(c => c.Identifier), Is.Unique);
        Assert.That(imported.ConnectorsConfiguration.Select(c => c.Identifier), Has.None.EqualTo(Guid.Empty));
        Assert.That(
          imported.ConnectorsConfiguration.Select(c => c.Identifier).Intersect(configuration.ConnectorsConfiguration.Select(c => c.Identifier)),
          Is.Empty);
        Assert.That(imported.ConnectorsConfiguration.Select(c => c.Name), Is.EqualTo(["First", "Second"]));
      });
    }

    [Test]
    public void LoadingMissingConfigurationReturnsDefaultsWithoutWritingAFile()
    {
      var configuration = this.serializer.LoadOrCreateConfiguration();

      Assert.That(configuration.ConnectorsConfiguration, Is.Empty);
      Assert.That(configuration.NotificationConfiguration.FailureNotificationEnabled, Is.True);
      Assert.That(File.Exists(this.serializer.ConfigFile), Is.False);
    }

    [Test]
    public void LoadingExistingConfigurationDoesNotReplaceItWithDefaults()
    {
      this.serializer.SaveConfiguration(new ApplicationConfiguration { ThemeMode = ApplicationTheme.Dark });

      Assert.That(this.serializer.LoadOrCreateConfiguration().ThemeMode, Is.EqualTo(ApplicationTheme.Dark));
    }

    [TestCase("{broken json")]
    [TestCase("[1,2,3]")]
    public void CorruptConfigurationIsReportedAndNeverOverwritten(string contents)
    {
      File.WriteAllText(this.serializer.ConfigFile, contents);

      Assert.That(() => this.serializer.LoadOrCreateConfiguration(), Throws.InstanceOf<JsonException>());
      Assert.That(File.ReadAllText(this.serializer.ConfigFile), Is.EqualTo(contents));
    }

    [Test]
    public void LoadingRemovesConsecutiveNullProjectsAndPreservesValidProjectsInOrder()
    {
      File.WriteAllText(
        this.serializer.ConfigFile,
        "{\"ConnectorsConfiguration\":[null,null,{\"Type\":\"Jenkins\",\"Name\":\"First\"},null,{\"Type\":\"Jenkins\",\"Name\":\"Second\"},null]}");

      var configuration = this.serializer.LoadConfiguration();

      Assert.That(configuration.ConnectorsConfiguration.Select(c => c.Name), Is.EqualTo(["First", "Second"]));
    }

    [TestCase("Soloplan.WhatsON.Jenkins.JenkinsPlugin", "Jenkins")]
    [TestCase("Soloplan.WhatsON.CruiseControl.CruiseControlPlugin", "CruiseControl")]
    public void LegacyPluginTypeNamesStillLoad(string legacyType, string expectedType)
    {
      File.WriteAllText(
        this.serializer.ConfigFile,
        JsonConvert.SerializeObject(new { ConnectorsConfiguration = new[] { new { PluginTypeName = legacyType, Name = "Legacy project" } }, }));

      var connector = this.serializer.LoadConfiguration().ConnectorsConfiguration.Single();

      Assert.That(connector.Type, Is.EqualTo(expectedType));
      Assert.That(connector.Name, Is.EqualTo("Legacy project"));
    }

    [Test]
    public void LegacyCruiseControlAddressMigrationRetainsTheProjectUrlAndIsIdempotent()
    {
      const string projectAddress = "https://builds.example/ccnet/server/local/project/Release/ViewProjectReport.aspx";
      var configuration = new ApplicationConfiguration();
      configuration.ConnectorsConfiguration.Add(new ConnectorConfiguration("CruiseControl", "Release", Connector.ServerAddress, projectAddress));
      this.serializer.SaveConfiguration(configuration);

      var migrated = this.serializer.LoadConfiguration();
      this.serializer.SaveConfiguration(migrated);
      var reloaded = this.serializer.LoadConfiguration().ConnectorsConfiguration.Single();

      Assert.Multiple(() =>
      {
        Assert.That(reloaded.GetConfigurationByKey(Connector.ServerAddress).Value, Is.EqualTo("https://builds.example/ccnet/"));
        Assert.That(reloaded.GetConfigurationByKey(Connector.DirectAddress).Value, Is.EqualTo(projectAddress));
        Assert.That(reloaded.ConfigurationItems.Count(c => c.Key == Connector.DirectAddress), Is.EqualTo(1));
      });
    }

    [Test]
    public void MigrationPreservesAnExplicitCruiseControlProjectAddress()
    {
      var configuration = new ApplicationConfiguration();
      configuration.ConnectorsConfiguration.Add(
        new ConnectorConfiguration(
          "CruiseControl",
          "Release",
          Connector.ServerAddress,
          "https://builds.example/ccnet/",
          Connector.DirectAddress,
          "https://other.example/project"));
      this.serializer.SaveConfiguration(configuration);

      var connector = this.serializer.LoadConfiguration().ConnectorsConfiguration.Single();

      Assert.That(connector.GetConfigurationByKey(Connector.DirectAddress).Value, Is.EqualTo("https://other.example/project"));
      Assert.That(connector.GetConfigurationByKey(Connector.ServerAddress).Value, Is.EqualTo("https://builds.example/ccnet/"));
    }

    [Test]
    public void SavingCreatesAMissingConfigurationDirectory()
    {
      Directory.Delete(this.directory);

      this.serializer.SaveConfiguration(new ApplicationConfiguration { ShowInTaskbar = true });

      Assert.That(this.serializer.LoadConfiguration().ShowInTaskbar, Is.True);
    }

    [Test]
    public void SavingToAnExplicitFileDoesNotOverwriteTheActiveConfiguration()
    {
      this.serializer.SaveConfiguration(new ApplicationConfiguration { ThemeMode = ApplicationTheme.Dark });
      var exportFile = Path.Combine(this.directory, "export.json");

      this.serializer.SaveConfiguration(new ApplicationConfiguration { ThemeMode = ApplicationTheme.Light }, exportFile);

      Assert.That(this.serializer.LoadConfiguration().ThemeMode, Is.EqualTo(ApplicationTheme.Dark));
      Assert.That(SerializationHelper.Load<ApplicationConfiguration>(exportFile).ThemeMode, Is.EqualTo(ApplicationTheme.Light));
    }

    [Test]
    public void SaveFailureIsReportedWithoutDamagingOtherConfigurationFiles()
    {
      this.serializer.SaveConfiguration(new ApplicationConfiguration { ThemeMode = ApplicationTheme.Dark });

      Assert.That(
        () => this.serializer.SaveConfiguration(new ApplicationConfiguration(), this.directory),
        Throws.InstanceOf<IOException>().Or.InstanceOf<UnauthorizedAccessException>());
      Assert.That(this.serializer.LoadConfiguration().ThemeMode, Is.EqualTo(ApplicationTheme.Dark));
    }
  }
}
