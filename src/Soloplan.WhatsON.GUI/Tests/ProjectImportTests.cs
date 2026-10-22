// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProjectImportTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Tests
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Threading.Tasks;
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Configuration;
  using Soloplan.WhatsON.Model;

  [TestFixture]
  [NonParallelizable]
  public class ProjectImportTests
  {
    private readonly string directory = Path.Combine(Path.GetTempPath(), "whatson-import-tests-" + Guid.NewGuid());

    private string previousConfigFolder = SerializationHelper.Instance.ConfigFolder;

    [SetUp]
    public void SetUp()
    {
      this.previousConfigFolder = SerializationHelper.Instance.ConfigFolder;
      SerializationHelper.Instance.SetConfigFolder(this.directory);
    }

    [TearDown]
    public void TearDown()
    {
      SerializationHelper.Instance.SetConfigFolder(this.previousConfigFolder);
      Directory.Delete(this.directory, true);
    }

    [Test]
    public async Task ImportedProjectsSurviveRestartAlongsideExistingProjectsAndPreferences()
    {
      var configuration = ExistingConfiguration();
      var existingProject = configuration.ConnectorsConfiguration.Single();
      var importedProjects = new[] { "release/net", "release/installer", "release/image" }
        .Select(name => new ConnectorConfiguration(
          "Jenkins",
          name,
          [
            new ConfigurationItem(Connector.ServerAddress, "https://build.example.invalid/"),
            new ConfigurationItem(Connector.ProjectName, "product/" + name),
            new ConfigurationItem("RedirectPlugin", "True"),
            new ConfigurationItem(Connector.Category, "Product release"),
          ])).ToList();
      var filePath = Path.Combine(this.directory, "projects.json");
      await SerializationHelper.Instance.SaveAsync(importedProjects, filePath, false);

      await new ProjectsDataInterchange().ImportAsync(configuration, filePath);

      var restartedSerializer = new SerializationHelper();
      restartedSerializer.SetConfigFolder(this.directory);
      var restored = await restartedSerializer.LoadOrCreateConfigurationAsync();
      Assert.That(restored.ConnectorsConfiguration.Select(project => project.Name), Is.EqualTo(["Existing", "release/net", "release/installer", "release/image"]));
      Assert.That(restored.ConnectorsConfiguration.Select(project => project.Identifier), Is.EqualTo(configuration.ConnectorsConfiguration.Select(project => project.Identifier)));
      Assert.That(restored.ConnectorsConfiguration.Select(project => project.Identifier), Is.Unique);
      Assert.That(restored.ConnectorsConfiguration.First().Identifier, Is.EqualTo(existingProject.Identifier));
      Assert.That(restored.ThemeMode, Is.EqualTo(ApplicationTheme.Dark));
      Assert.That(restored.Language, Is.EqualTo(ApplicationLanguage.German));
      Assert.That(restored.NotificationConfiguration.OnlyIfChanged, Is.True);
      foreach (var project in restored.ConnectorsConfiguration.Skip(1))
      {
        Assert.That(project.GetConfigurationByKey(Connector.ProjectName).Value, Is.EqualTo("product/" + project.Name));
        Assert.That(project.GetConfigurationByKey("RedirectPlugin").Value, Is.EqualTo("True"));
        Assert.That(project.GetConfigurationByKey(Connector.Category).Value, Is.EqualTo("Product release"));
      }
    }

    [TestCase("{broken json")]
    [TestCase("null")]
    [TestCase("[null]")]
    public async Task InvalidImportLeavesExistingConfigurationUntouched(string json)
    {
      var configuration = ExistingConfiguration();
      await SerializationHelper.Instance.SaveConfigurationAsync(configuration);
      var saved = await File.ReadAllTextAsync(SerializationHelper.Instance.ConfigFile);
      var existingProject = configuration.ConnectorsConfiguration.Single();
      var filePath = Path.Combine(this.directory, "invalid-projects.json");
      await File.WriteAllTextAsync(filePath, json);

      await Assert.ThatAsync(() => new ProjectsDataInterchange().ImportAsync(configuration, filePath), Throws.Exception);

      Assert.That(configuration.ConnectorsConfiguration, Is.EqualTo([existingProject]));
      Assert.That(await File.ReadAllTextAsync(SerializationHelper.Instance.ConfigFile), Is.EqualTo(saved));
    }

    [Test]
    public async Task SaveFailureDoesNotLeaveAPartiallyImportedConfiguration()
    {
      var configuration = ExistingConfiguration();
      await SerializationHelper.Instance.SaveConfigurationAsync(configuration);
      var saved = await File.ReadAllTextAsync(SerializationHelper.Instance.ConfigFile);
      var existingProject = configuration.ConnectorsConfiguration.Single();
      var filePath = Path.Combine(this.directory, "projects.json");
      await SerializationHelper.Instance.SaveAsync(new[] { new ConnectorConfiguration("Jenkins", "Imported") }, filePath, false);

      using (var lockedConfiguration = File.Open(SerializationHelper.Instance.ConfigFile, FileMode.Open, FileAccess.Read, FileShare.Read))
      {
        await Assert.ThatAsync(() => new ProjectsDataInterchange().ImportAsync(configuration, filePath), Throws.InstanceOf<IOException>().Or.InstanceOf<UnauthorizedAccessException>());
      }

      Assert.That(configuration.ConnectorsConfiguration, Is.EqualTo([existingProject]));
      Assert.That(await File.ReadAllTextAsync(SerializationHelper.Instance.ConfigFile), Is.EqualTo(saved));
    }

    private static ApplicationConfiguration ExistingConfiguration()
    {
      var configuration = new ApplicationConfiguration { ThemeMode = ApplicationTheme.Dark, Language = ApplicationLanguage.German };
      configuration.NotificationConfiguration.OnlyIfChanged = true;
      configuration.ConnectorsConfiguration.Add(new ConnectorConfiguration("Jenkins", "Existing"));
      return configuration;
    }
  }
}
