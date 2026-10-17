// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ThemeTests.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Tests
{
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Configuration.ViewModel;

  [TestFixture]
  public class ThemeTests
  {
    [TestCase(ApplicationTheme.System, false, false)]
    [TestCase(ApplicationTheme.System, true, true)]
    [TestCase(ApplicationTheme.Light, false, false)]
    [TestCase(ApplicationTheme.Light, true, false)]
    [TestCase(ApplicationTheme.Dark, false, true)]
    [TestCase(ApplicationTheme.Dark, true, true)]
    public void ExplicitThemeOverridesWindowsAndSystemFollowsWindows(ApplicationTheme mode, bool systemIsDark, bool expectedDark)
    {
      Assert.That(ThemeHelper.IsDarkMode(mode, systemIsDark), Is.EqualTo(expectedDark));
    }

    [TestCase(ApplicationTheme.System)]
    [TestCase(ApplicationTheme.Light)]
    [TestCase(ApplicationTheme.Dark)]
    public void EditingAndResettingAppearancePreservesTheSavedPreference(ApplicationTheme mode)
    {
      var source = new ApplicationConfiguration { ThemeMode = ApplicationTheme.Light };
      var model = new ConfigViewModel();
      model.Load(source);
      Assert.That(model.ConfigurationIsModified, Is.False);

      model.ThemeMode = mode;
      Assert.That(model.ConfigurationIsModified, Is.EqualTo(mode != source.ThemeMode));
      Assert.That(source.ThemeMode, Is.EqualTo(ApplicationTheme.Light));

      var exported = new ApplicationConfiguration();
      model.ApplyToConfiguration(exported);
      Assert.That(exported.ThemeMode, Is.EqualTo(mode));

      model.Load(source);
      Assert.That(model.ThemeMode, Is.EqualTo(ApplicationTheme.Light));
      Assert.That(model.ConfigurationIsModified, Is.False);
    }
  }
}
