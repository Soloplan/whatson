// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ThemeConfigurationTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Tests
{
  using Newtonsoft.Json;
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;

  [TestFixture]
  public class ThemeConfigurationTests
  {
    [Test]
    public void NewConfigurationFollowsSystemAppearance()
    {
      Assert.That(new ApplicationConfiguration().ThemeMode, Is.EqualTo(ApplicationTheme.System));
      Assert.That((JsonConvert.DeserializeObject<ApplicationConfiguration>("{}") ?? throw new AssertionException("Deserialization returned null.")).ThemeMode, Is.EqualTo(ApplicationTheme.System));
    }

    [TestCase("{\"DarkThemeEnabled\":true}", ApplicationTheme.Dark)]
    [TestCase("{\"DarkThemeEnabled\":false}", ApplicationTheme.Light)]
    [TestCase("{\"ThemeMode\":\"System\",\"DarkThemeEnabled\":true}", ApplicationTheme.System)]
    [TestCase("{\"DarkThemeEnabled\":true,\"ThemeMode\":\"Light\"}", ApplicationTheme.Light)]
    [TestCase("{\"ThemeMode\":\"Dark\",\"DarkThemeEnabled\":false}", ApplicationTheme.Dark)]
    public void LegacyPreferenceIsMigratedWithoutOverridingAnExplicitTheme(string json, ApplicationTheme expected) => Assert.That((JsonConvert.DeserializeObject<ApplicationConfiguration>(json) ?? throw new AssertionException("Deserialization returned null.")).ThemeMode, Is.EqualTo(expected));

    [TestCase(ApplicationTheme.System)]
    [TestCase(ApplicationTheme.Light)]
    [TestCase(ApplicationTheme.Dark)]
    public void ThemePreferenceRoundTripsWithoutWritingTheLegacyFlag(ApplicationTheme mode)
    {
      var json = JsonConvert.SerializeObject(new ApplicationConfiguration { ThemeMode = mode });
      Assert.That(json, Does.Contain($"\"ThemeMode\":\"{mode}\""));
      Assert.That(json, Does.Not.Contain("DarkThemeEnabled"));
      Assert.That((JsonConvert.DeserializeObject<ApplicationConfiguration>(json) ?? throw new AssertionException("Deserialization returned null.")).ThemeMode, Is.EqualTo(mode));
    }
  }
}
