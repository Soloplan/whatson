// <copyright file="LocalizationTests.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.Tests
{
  using System;
  using System.Collections;
  using System.Globalization;
  using System.Linq;
  using System.Text.RegularExpressions;
  using Newtonsoft.Json;
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Localization;
  using Soloplan.WhatsON.Model;

  [TestFixture]
  [NonParallelizable]
  public class LocalizationTests
  {
    [Test]
    public void ExistingAndNewConfigurationsFollowTheSystemLanguage()
    {
      Assert.That(new ApplicationConfiguration().Language, Is.EqualTo(ApplicationLanguage.System));
      Assert.That(JsonConvert.DeserializeObject<ApplicationConfiguration>("{}").Language, Is.EqualTo(ApplicationLanguage.System));
    }

    [TestCase(ApplicationLanguage.English)]
    [TestCase(ApplicationLanguage.German)]
    [TestCase(ApplicationLanguage.System)]
    public void LanguagePreferenceSurvivesSerialization(ApplicationLanguage language)
    {
      var json = JsonConvert.SerializeObject(new ApplicationConfiguration { Language = language });
      Assert.That(json, Does.Contain($"\"Language\":\"{language}\""));
      Assert.That(JsonConvert.DeserializeObject<ApplicationConfiguration>(json).Language, Is.EqualTo(language));
    }

    [Test]
    public void GermanCatalogHasEveryEnglishKeyAndPreservesFormattingArguments()
    {
      var manager = Strings.ResourceManager;
      var english = manager.GetResourceSet(CultureInfo.InvariantCulture, true, false);
      var german = manager.GetResourceSet(CultureInfo.GetCultureInfo("de"), true, false);
      Assert.That(german, Is.Not.Null, "A German satellite assembly must be deployed.");
      Assert.That(german.Cast<DictionaryEntry>().Select(entry => entry.Key), Is.EquivalentTo(english.Cast<DictionaryEntry>().Select(entry => entry.Key)));
      foreach (DictionaryEntry entry in english)
      {
        var translation = german.GetString((string)entry.Key);
        Assert.That(translation, Is.Not.Null.And.Not.Empty, (string)entry.Key);
        var expectedArguments = Regex.Matches((string)entry.Value, @"\{\d+\}").Select(match => match.Value);
        var actualArguments = Regex.Matches(translation, @"\{\d+\}").Select(match => match.Value);
        Assert.That(actualArguments, Is.EquivalentTo(expectedArguments), (string)entry.Key);
      }
    }

    [Test]
    [SetUICulture("de-AT")]
    public void GermanRegionalCulturesUseTheGermanTranslationAndPreserveUserText()
    {
      Assert.That(Strings.Settings, Is.EqualTo("Einstellungen"));
      Assert.That(Localizer.DeleteNamedProject("Build {42}"), Is.EqualTo("Möchten Sie das Projekt „Build {42}“ wirklich löschen?"));
    }

    [Test]
    [SetUICulture("fr-FR")]
    public void UnsupportedCulturesFallBackToEnglish()
    {
      Assert.That(Strings.Settings, Is.EqualTo("Settings"));
    }

    [TestCase("en-US", ObservationState.Unknown, "Unknown")]
    [TestCase("en-US", ObservationState.Unstable, "Unstable")]
    [TestCase("en-US", ObservationState.Failure, "Failure")]
    [TestCase("en-US", ObservationState.Success, "Success")]
    [TestCase("en-US", ObservationState.Running, "Running")]
    [TestCase("de-DE", ObservationState.Unknown, "Unbekannt")]
    [TestCase("de-DE", ObservationState.Unstable, "Instabil")]
    [TestCase("de-DE", ObservationState.Failure, "Fehlgeschlagen")]
    [TestCase("de-DE", ObservationState.Success, "Erfolgreich")]
    [TestCase("de-DE", ObservationState.Running, "Läuft")]
    [TestCase("de-DE", (ObservationState)99, "Unbekannt")]
    public void StateNamesUseTypedStatesAndTheSelectedCulture(string culture, ObservationState state, string expected)
    {
      var originalCulture = CultureInfo.CurrentUICulture;
      try
      {
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
        Assert.That(Localizer.GetStateName(state), Is.EqualTo(expected));
      }
      finally
      {
        CultureInfo.CurrentUICulture = originalCulture;
      }
    }

    [TestCase("en-US")]
    [TestCase("de-DE")]
    public void NamedMessagesKeepArgumentsInOrderAndPreserveUserText(string culture)
    {
      var originalCulture = CultureInfo.CurrentUICulture;
      try
      {
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
        const string name = "Build {42}";
        const string path = @"C:\builds\{project}.json";
        const string error = "Invalid {response}";
        var identifier = Guid.Parse("4c8daa45-2587-4f0b-a7ae-6ca95f1ec862");
        var german = culture == "de-DE";
        Assert.That(Localizer.NotificationSettings(name), Is.EqualTo(german ? $"Benachrichtigungen ({name})" : $"Notification settings ({name})"));
        Assert.That(Localizer.ProjectNotification(name), Is.EqualTo(german ? $"Projektname: {name}." : $"Project name: {name}."));
        Assert.That(Localizer.DeleteGroup(name), Is.EqualTo(german ? $"Möchten Sie die Gruppe „{name}“ und alle zugehörigen Projekte wirklich löschen?" : $"Are you sure you want to delete the group '{name}' and all its items?"));
        Assert.That(Localizer.DeleteNamedProject(name), Is.EqualTo(german ? $"Möchten Sie das Projekt „{name}“ wirklich löschen?" : $"Are you sure you want to delete the project '{name}'?"));
        Assert.That(Localizer.ProjectError(error).Replace("\r\n", "\n"), Is.EqualTo(german ? $"Beim Laden der Projekte ist ein Fehler aufgetreten.\nDetails: {error}" : $"There was a project error,\ndetails: {error}"));
        Assert.That(Localizer.ImportFailed(path, error), Is.EqualTo(german ? $"Die Konfiguration konnte nicht aus der JSON-Datei importiert werden. Dateipfad: {path}; Fehler: {error}" : $"Import of the configuration from JSON file was not successful; file path: {path}; exception: {error}"));
        Assert.That(Localizer.ProjectImportFailed(path, error), Is.EqualTo(german ? $"Die Projekte konnten nicht aus der JSON-Datei importiert werden. Dateipfad: {path}; Fehler: {error}" : $"Import of the projects configuration from JSON file was not successful; file path: {path}; exception: {error}"));
        Assert.That(Localizer.MissingConnectorData(identifier, name, error, "Jenkins").Replace("\r\n", "\n"), Is.EqualTo(german ? $"Kennung: {identifier}\nName: {name}\nBeschreibung: {error}\nErwarteter Verbindungstyp: Jenkins" : $"Identifier: {identifier}\nName: {name}\nDescription: {error}\nExpected connector type: Jenkins"));
      }
      finally
      {
        CultureInfo.CurrentUICulture = originalCulture;
      }
    }

    [TestCase(ApplicationLanguage.System, "de-DE", "de-DE")]
    [TestCase(ApplicationLanguage.System, "de-AT", "de-AT")]
    [TestCase(ApplicationLanguage.System, "de-CH", "de-CH")]
    [TestCase(ApplicationLanguage.System, "en-US", "en-US")]
    [TestCase(ApplicationLanguage.System, "en-GB", "en-GB")]
    [TestCase(ApplicationLanguage.System, "fr-FR", "en-US")]
    [TestCase(ApplicationLanguage.System, "ja-JP", "en-US")]
    [TestCase(ApplicationLanguage.System, "", "en-US")]
    [TestCase(ApplicationLanguage.English, "de-DE", "en-US")]
    [TestCase(ApplicationLanguage.German, "en-US", "de-DE")]
    [TestCase((ApplicationLanguage)99, "de-DE", "en-US")]
    public void SavedLanguageAppliesToUIFormattingAndBackgroundThreads(ApplicationLanguage language, string systemCulture, string cultureName)
    {
      var currentCulture = CultureInfo.CurrentCulture;
      var currentUICulture = CultureInfo.CurrentUICulture;
      var defaultCulture = CultureInfo.DefaultThreadCurrentCulture;
      var defaultUICulture = CultureInfo.DefaultThreadCurrentUICulture;
      try
      {
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(systemCulture);
        Localizer.ApplyLanguage(language);
        Assert.That(CultureInfo.CurrentCulture.Name, Is.EqualTo(cultureName));
        Assert.That(CultureInfo.CurrentUICulture.Name, Is.EqualTo(cultureName));
        Assert.That(CultureInfo.DefaultThreadCurrentCulture.Name, Is.EqualTo(cultureName));
        Assert.That(CultureInfo.DefaultThreadCurrentUICulture.Name, Is.EqualTo(cultureName));
      }
      finally
      {
        CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
        CultureInfo.DefaultThreadCurrentUICulture = defaultUICulture;
        CultureInfo.CurrentCulture = currentCulture;
        CultureInfo.CurrentUICulture = currentUICulture;
      }
    }
  }
}
