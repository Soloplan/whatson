// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TrayUpdateTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Tests
{
  using System;
  using System.Globalization;
  using System.Reflection;
  using System.Threading;
  using System.Threading.Tasks;
  using System.Windows.Forms;
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Windows.ApplicationModel;

  [TestFixture]
  [Apartment(ApartmentState.STA)]
  [SetUICulture("en-US")]
  public class TrayUpdateTests
  {
    [TestCase(PackageUpdateAvailability.Available, "https://updates.example.invalid/whatsON.appinstaller", true)]
    [TestCase(PackageUpdateAvailability.Required, "https://updates.example.invalid/whatsON.appinstaller", true)]
    [TestCase(PackageUpdateAvailability.NoUpdates, "https://updates.example.invalid/whatsON.appinstaller", false)]
    [TestCase(PackageUpdateAvailability.Unknown, "https://updates.example.invalid/whatsON.appinstaller", false)]
    [TestCase(PackageUpdateAvailability.Error, "https://updates.example.invalid/whatsON.appinstaller", false)]
    [TestCase(PackageUpdateAvailability.Available, "http://updates.example.invalid/whatsON.appinstaller", false)]
    [TestCase(PackageUpdateAvailability.Available, "file:///C:/whatsON.appinstaller", false)]
    [TestCase(PackageUpdateAvailability.Available, null, false)]
    public void OnlyAvailableUpdatesFromHttpsFeedsAddARestartAction(PackageUpdateAvailability availability, string? feed, bool available)
    {
      using var tray = new TrayHandler(new ObservationScheduler(), new ApplicationConfiguration());
      Field("updateRestartReadyAt").SetValue(tray, 0L);
      var menu = (ContextMenuStrip)(Field("contextMenu").GetValue(tray) ?? throw new AssertionException("Missing menu."));
      menu.Items.Add("Open window");
      menu.Items.Add("Exit");
      var installerUri = feed == null ? null : new Uri(feed);
      Method("SetUpdateAvailability").Invoke(tray, new object?[] { availability, installerUri });
      Method("UpdateRestartMenu").Invoke(tray, null);
      Assert.That(Field("updateInstallerUri").GetValue(tray), Is.EqualTo(available ? installerUri : null));
      Assert.That(menu.Items.Count, Is.EqualTo(available ? 3 : 2), "Refreshing must not duplicate the restart action.");
      Assert.That(menu.Items[menu.Items.Count - 1].Text, Is.EqualTo("Exit"));
      if (available)
      {
        Assert.That(menu.Items[1].Text, Is.EqualTo("Restart to update"));
        Assert.That(menu.Items[1].Enabled, Is.True);
      }

      Method("SetUpdateAvailability").Invoke(tray, new object?[] { PackageUpdateAvailability.NoUpdates, installerUri });
      Assert.That(menu.Items.Count, Is.EqualTo(2), "The restart action must disappear when no update is available.");
    }

    [Test]
    public void RestartWaitsForWindowsAndFollowsTheCurrentLanguageAndInstallationState()
    {
      using var tray = new TrayHandler(new ObservationScheduler(), new ApplicationConfiguration());
      Field("updateRestartReadyAt").SetValue(tray, long.MaxValue);
      var menu = (ContextMenuStrip)(Field("contextMenu").GetValue(tray) ?? throw new AssertionException("Missing menu."));
      menu.Items.Add("Exit");
      Method("SetUpdateAvailability").Invoke(tray, new object[] { PackageUpdateAvailability.Available, new Uri("https://updates.example.invalid/whatsON.appinstaller") });
      Assert.That(menu.Items.Count, Is.EqualTo(1));
      Field("updateRestartReadyAt").SetValue(tray, 0L);
      Method("UpdateRestartMenu").Invoke(tray, null);
      Assert.That(menu.Items[0].Text, Is.EqualTo("Restart to update"));

      var originalCulture = CultureInfo.CurrentUICulture;
      try
      {
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
        Field("updating").SetValue(tray, true);
        Method("UpdateRestartMenu").Invoke(tray, null);
        Assert.That(menu.Items[0].Text, Is.EqualTo("Zum Aktualisieren neu starten"));
        Assert.That(menu.Items[0].Enabled, Is.False);
        Field("updating").SetValue(tray, false);
        Method("UpdateRestartMenu").Invoke(tray, null);
        Assert.That(menu.Items[0].Enabled, Is.True);
      }
      finally
      {
        CultureInfo.CurrentUICulture = originalCulture;
      }
    }

    [Test]
    public async Task DisposedTrayIgnoresPendingUpdateChecks()
    {
      var tray = new TrayHandler(new ObservationScheduler(), new ApplicationConfiguration());
      tray.Dispose();
      await ((Task)(Method("CheckForUpdatesAsync").Invoke(tray, null) ?? throw new AssertionException("Missing task.")));
      Assert.That(Field("updateInstallerUri").GetValue(tray), Is.Null);
    }

    private static FieldInfo Field(string name) => typeof(TrayHandler).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
      ?? throw new AssertionException($"Missing field {name}.");

    private static MethodInfo Method(string name) => typeof(TrayHandler).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
      ?? throw new AssertionException($"Missing method {name}.");
  }
}
