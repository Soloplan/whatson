// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TrayHandler.Updates.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI
{
  using System;
  using System.Diagnostics;
  using System.IO;
  using System.Runtime.InteropServices;
  using System.Threading.Tasks;
  using System.Windows.Forms;
  using Soloplan.WhatsON.Localization;
  using Windows.ApplicationModel;
  using Windows.Management.Deployment;

  public partial class TrayHandler
  {
    private readonly ToolStripMenuItem restartToUpdateMenu = new ToolStripMenuItem();

    private readonly Timer updateTimer = new Timer { Interval = 60000 };

    // Windows only restarts processes that have been running for at least one minute.
    private readonly long updateRestartReadyAt = Stopwatch.GetTimestamp() + (60 * Stopwatch.Frequency);

    private DateTime nextUpdateCheck;

    private bool checkingForUpdates;

    private bool updating;

    private Uri? updateInstallerUri;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int RegisterApplicationRestart(string commandLine, int flags);

    [DllImport("kernel32.dll")]
    private static extern int UnregisterApplicationRestart();

    private async Task CheckForUpdatesAsync()
    {
      if (this.icon == null)
      {
        return;
      }

      this.UpdateRestartMenu();
      if (this.checkingForUpdates || this.updating || DateTime.UtcNow < this.nextUpdateCheck)
      {
        return;
      }

      this.checkingForUpdates = true;
      this.nextUpdateCheck = DateTime.UtcNow.AddMinutes(15);
      try
      {
        var package = new PackageManager().FindPackageForUser(string.Empty, Package.Current.Id.FullName);
        var installerUri = package.GetAppInstallerInfo()?.Uri;
        if (installerUri?.Scheme != Uri.UriSchemeHttps)
        {
          this.SetUpdateAvailability(PackageUpdateAvailability.Unknown, null);
          return;
        }

        var result = await package.CheckUpdateAvailabilityAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(30));
        if (this.icon != null)
        {
          this.SetUpdateAvailability(result.Availability, installerUri);
        }
      }
      catch (Exception exception)
      {
        // Portable launches and offline feeds must not interrupt monitoring.
        NLog.LogManager.GetCurrentClassLogger().Debug(exception, "Could not check for a whatsON update.");
      }
      finally
      {
        this.checkingForUpdates = false;
      }
    }

    private void SetUpdateAvailability(PackageUpdateAvailability availability, Uri? installerUri)
    {
      this.updateInstallerUri = installerUri?.Scheme == Uri.UriSchemeHttps
        && availability is PackageUpdateAvailability.Available or PackageUpdateAvailability.Required
          ? installerUri : null;
      this.UpdateRestartMenu();
    }

    private void UpdateRestartMenu()
    {
      if (this.contextMenu == null)
      {
        return;
      }

      this.restartToUpdateMenu.Text = Strings.RestartToUpdate;
      this.restartToUpdateMenu.Enabled = !this.updating;
      if (this.updateInstallerUri != null && Stopwatch.GetTimestamp() >= this.updateRestartReadyAt)
      {
        if (!this.contextMenu.Items.Contains(this.restartToUpdateMenu))
        {
          this.contextMenu.Items.Insert(Math.Max(0, this.contextMenu.Items.Count - 1), this.restartToUpdateMenu);
        }
      }
      else
      {
        this.contextMenu.Items.Remove(this.restartToUpdateMenu);
      }
    }

    private async Task RestartToUpdateAsync()
    {
      var installerUri = this.updateInstallerUri;
      if (installerUri == null || this.updating || Stopwatch.GetTimestamp() < this.updateRestartReadyAt
        || MessageBox.Show(Strings.RestartToUpdateConfirmation, Strings.RestartToUpdate, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
      {
        return;
      }

      this.updating = true;
      this.UpdateRestartMenu();
      try
      {
        if (this.mainWindow != null)
        {
          this.VisualSettings = this.mainWindow.GetVisualSettings();
        }

        SerializationHelper.Instance.Save(this.VisualSettings, Path.Combine(SerializationHelper.Instance.ConfigFolder, MainWindow.VisualSettingsFile));

        // A final dot prevents a trailing backslash from escaping the command-line quote.
        var restartArguments = $"\"/configDir:{Path.Combine(SerializationHelper.Instance.ConfigFolder, ".")}\"";

        // Restart for the package update, never for a crash, hang, or Windows reboot.
        Marshal.ThrowExceptionForHR(RegisterApplicationRestart(restartArguments, 1 | 2 | 8));
        var result = await new PackageManager().AddPackageByAppInstallerFileAsync(installerUri, AddPackageByAppInstallerOptions.ForceTargetAppShutdown, null);
        if (result.ExtendedErrorCode != null)
        {
          throw result.ExtendedErrorCode;
        }
      }
      catch (Exception exception)
      {
        var unregisterResult = UnregisterApplicationRestart();
        if (unregisterResult < 0)
        {
          NLog.LogManager.GetCurrentClassLogger().Warn("Could not unregister the update restart: HRESULT 0x{0:X8}", unregisterResult);
        }

        NLog.LogManager.GetCurrentClassLogger().Error(exception, "Could not install the whatsON update.");
        if (this.icon != null)
        {
          MessageBox.Show(Strings.UpdateFailedMessage, Strings.UpdateFailed, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
      }
      finally
      {
        this.updating = false;
        this.UpdateRestartMenu();
      }
    }
  }
}
