// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LocalizationTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Tests
{
  using System;
  using System.Globalization;
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common.Converters;
  using Soloplan.WhatsON.GUI.Configuration;
  using Soloplan.WhatsON.GUI.Configuration.ViewModel;
  using Soloplan.WhatsON.Jenkins;
  using Soloplan.WhatsON.Model;

  [TestFixture]
  [NonParallelizable]
  public class LocalizationTests
  {
    [Test]
    public void EditingResettingAndExportingLanguagePreservesTheSavedChoiceUntilApplied()
    {
      var source = new ApplicationConfiguration();
      var model = new ConfigViewModel();
      model.Load(source);
      var uiCulture = CultureInfo.CurrentUICulture;
      model.Language = ApplicationLanguage.German;
      Assert.That(model.ConfigurationIsModified, Is.True);
      Assert.That(source.Language, Is.EqualTo(ApplicationLanguage.System));
      Assert.That(CultureInfo.CurrentUICulture, Is.EqualTo(uiCulture), "Changing the selector must not leave open views partially translated.");
      var exported = new ApplicationConfiguration();
      model.ApplyToConfiguration(exported);
      Assert.That(exported.Language, Is.EqualTo(ApplicationLanguage.German));
      model.Load(source);
      Assert.That(model.Language, Is.EqualTo(ApplicationLanguage.System));
      Assert.That(model.ConfigurationIsModified, Is.False);
    }

    [Test]
    [SetUICulture("de-DE")]
    public void ValidationTrayMessagesAndRelativeTimesUseGerman()
    {
      Assert.That(Properties.Resources.ValueIsRequired, Is.EqualTo("Ein Wert ist erforderlich."));
      Assert.That(Properties.Resources.TrayHandler_UpdateTrayIcon_NoProjects, Is.EqualTo("WhatsON - Keine Projekte konfiguriert"));
      var converter = new TimeToApproximateTimeConverter();
      Assert.That(converter.Convert(null, typeof(string), null, CultureInfo.CurrentCulture), Is.EqualTo("nie"));
      Assert.That(converter.Convert(DateTime.Now.AddMinutes(-5), typeof(string), null, CultureInfo.CurrentCulture), Is.EqualTo("vor 5 Minuten"));
    }

    [Test]
    public void ConnectorCaptionCacheKeepsCulturesSeparate()
    {
      var originalCulture = CultureInfo.CurrentUICulture;
      try
      {
        foreach (var language in new[] { "de-DE", "en-US", "de-DE" })
        {
          CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);
          var caption = new ConfigurationItemAttribute(Connector.Category, typeof(string));
          ConfigResourcesHelper.ApplyConfigResources(caption, typeof(JenkinsConnector));
          Assert.That(caption.Caption, Is.EqualTo(language == "de-DE" ? "Gruppe" : "Group"));
          caption = new ConfigurationItemAttribute(JenkinsConnector.RedirectPlugin, typeof(bool));
          ConfigResourcesHelper.ApplyConfigResources(caption, typeof(JenkinsConnector));
          Assert.That(caption.Caption, Is.EqualTo(language == "de-DE" ? "Blue-Ocean-URL verwenden" : "Use Blue Ocean URL"));
        }
      }
      finally
      {
        CultureInfo.CurrentUICulture = originalCulture;
      }
    }
  }
}
