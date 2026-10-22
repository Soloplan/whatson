// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LocalizationTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

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
      Assert.That((JsonConvert.DeserializeObject<ApplicationConfiguration>("{}") ?? throw new AssertionException("Deserialization returned null.")).Language, Is.EqualTo(ApplicationLanguage.System));
    }

    [TestCase(ApplicationLanguage.English)]
    [TestCase(ApplicationLanguage.German)]
    [TestCase(ApplicationLanguage.System)]
    public void LanguagePreferenceSurvivesSerialization(ApplicationLanguage language)
    {
      var json = JsonConvert.SerializeObject(new ApplicationConfiguration { Language = language });
      Assert.That(json, Does.Contain($"\"Language\":\"{language}\""));
      Assert.That((JsonConvert.DeserializeObject<ApplicationConfiguration>(json) ?? throw new AssertionException("Deserialization returned null.")).Language, Is.EqualTo(language));
    }

    [Test]
    public void GermanCatalogHasEveryEnglishKeyAndPreservesFormattingArguments()
    {
      var manager = Strings.ResourceManager;
      var english = manager.GetResourceSet(CultureInfo.InvariantCulture, true, false) ?? throw new AssertionException("Missing English resources.");
      var german = manager.GetResourceSet(CultureInfo.GetCultureInfo("de"), true, false) ?? throw new AssertionException("Missing German resources.");
      Assert.That(german, Is.Not.Null, "A German satellite assembly must be deployed.");
      Assert.That(
        german.Cast<DictionaryEntry>().Select(entry => entry.Key),
        Is.EquivalentTo(english.Cast<DictionaryEntry>().Select(entry => entry.Key)));
      foreach (DictionaryEntry entry in english)
      {
        var translation = german.GetString((string)entry.Key) ?? throw new AssertionException($"Missing translation: {entry.Key}");
        Assert.That(translation, Is.Not.Null.And.Not.Empty, (string)entry.Key);
        var expectedArguments = Regex.Matches(entry.Value as string ?? throw new AssertionException("Resource must be a string."), @"\{\d+\}").Select(match => match.Value);
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
    public void UnsupportedCulturesFallBackToEnglish() => Assert.That(Strings.Settings, Is.EqualTo("Settings"));

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
        const string Name = "Build {42}";
        const string Path = @"C:\builds\{project}.json";
        const string Error = "Invalid {response}";
        var identifier = Guid.Parse("4c8daa45-2587-4f0b-a7ae-6ca95f1ec862");
        var german = culture == "de-DE";
        Assert.That(Localizer.NotificationSettings(Name), Is.EqualTo(german ? $"Benachrichtigungen ({Name})" : $"Notification settings ({Name})"));
        Assert.That(Localizer.ProjectNotification(Name), Is.EqualTo(german ? $"Projektname: {Name}." : $"Project name: {Name}."));
        Assert.That(
          Localizer.DeleteGroup(Name),
          Is.EqualTo(
            german
              ? $"Möchten Sie die Gruppe „{Name}“ und alle zugehörigen Projekte wirklich löschen?"
              : $"Are you sure you want to delete the group '{Name}' and all its items?"));
        Assert.That(
          Localizer.DeleteNamedProject(Name),
          Is.EqualTo(german ? $"Möchten Sie das Projekt „{Name}“ wirklich löschen?" : $"Are you sure you want to delete the project '{Name}'?"));
        Assert.That(
          Localizer.ProjectError(Error).Replace("\r\n", "\n"),
          Is.EqualTo(
            german ? $"Beim Laden der Projekte ist ein Fehler aufgetreten.\nDetails: {Error}" : $"There was a project error,\ndetails: {Error}"));
        Assert.That(
          Localizer.ImportFailed(Path, Error),
          Is.EqualTo(
            german
              ? $"Die Konfiguration konnte nicht aus der JSON-Datei importiert werden. Dateipfad: {Path}; Fehler: {Error}"
              : $"Import of the configuration from JSON file was not successful; file path: {Path}; exception: {Error}"));
        Assert.That(
          Localizer.ProjectImportFailed(Path, Error),
          Is.EqualTo(
            german
              ? $"Die Projekte konnten nicht aus der JSON-Datei importiert werden. Dateipfad: {Path}; Fehler: {Error}"
              : $"Import of the projects configuration from JSON file was not successful; file path: {Path}; exception: {Error}"));
        Assert.That(
          Localizer.MissingConnectorData(identifier, Name, Error, "Jenkins").Replace("\r\n", "\n"),
          Is.EqualTo(
            german
              ? $"Kennung: {identifier}\nName: {Name}\nBeschreibung: {Error}\nErwarteter Verbindungstyp: Jenkins"
              : $"Identifier: {identifier}\nName: {Name}\nDescription: {Error}\nExpected connector type: Jenkins"));
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
    public void LanguageResolutionUsesSupportedSystemCulturesOrEnglish(ApplicationLanguage language, string systemCulture, string cultureName)
    {
      Assert.That(Localizer.ResolveCulture(language, CultureInfo.GetCultureInfo(systemCulture)).Name, Is.EqualTo(cultureName));
    }

    [Test]
    public void ChangingLanguageUpdatesFormattingAndBackgroundThreadsAndCanReturnToSystem()
    {
      var currentCulture = CultureInfo.CurrentCulture;
      var currentUiCulture = CultureInfo.CurrentUICulture;
      var defaultCulture = CultureInfo.DefaultThreadCurrentCulture;
      var defaultUiCulture = CultureInfo.DefaultThreadCurrentUICulture;
      try
      {
        Localizer.ApplyLanguage(ApplicationLanguage.System);
        var systemLanguage = CultureInfo.CurrentUICulture;
        foreach (var language in new[] { ApplicationLanguage.German, ApplicationLanguage.English, ApplicationLanguage.System })
        {
          var expected = language == ApplicationLanguage.System ? systemLanguage.Name : language == ApplicationLanguage.German ? "de-DE" : "en-US";
          Localizer.ApplyLanguage(language);
          Assert.That(CultureInfo.CurrentCulture.Name, Is.EqualTo(expected));
          Assert.That(CultureInfo.CurrentUICulture.Name, Is.EqualTo(expected));
          Assert.That(CultureInfo.DefaultThreadCurrentCulture?.Name, Is.EqualTo(expected));
          Assert.That(CultureInfo.DefaultThreadCurrentUICulture?.Name, Is.EqualTo(expected));
        }
      }
      finally
      {
        CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
        CultureInfo.DefaultThreadCurrentUICulture = defaultUiCulture;
        CultureInfo.CurrentCulture = currentCulture;
        CultureInfo.CurrentUICulture = currentUiCulture;
      }
    }
  }
}
