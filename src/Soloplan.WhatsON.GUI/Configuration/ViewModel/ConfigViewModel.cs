// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration.ViewModel
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading.Tasks;
  using System.Windows.Controls;
  using Microsoft.Win32;
  using NLog;
  using PropertyChanged;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Configuration.View;
  using Soloplan.WhatsON.Localization;

  /// <summary>
  /// The view model for see <see cref="Configuration" />.
  /// </summary>
  public class ConfigViewModel : ViewModelBase
  {
    /// <summary>
    /// The Windows Startup Keys registry location.
    /// </summary>
    private const string StartupSubKeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";

    /// <summary>
    /// The registry startup key name.
    /// </summary>
    private const string StartupKey = "WhatsON";

    /// <summary>
    /// Logger instance used by this class.
    /// </summary>
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// The open on system start.
    /// </summary>
    private bool? openOnSystemStart;

    /// <summary>
    /// Occurs when configuration was applied.
    /// </summary>
    public event EventHandler<ValueEventArgs<ApplicationConfiguration>>? ConfigurationApplied;

    /// <summary>
    /// Occurs when configuration is about to be applied.
    /// </summary>
    public event EventHandler<EventArgs>? ConfigurationApplying;

    /// <summary>
    /// Gets the connectors list.
    /// </summary>
    public ConnectorViewModelCollection Connectors { get; private set; } = new ConnectorViewModelCollection();

    public ConnectorViewModelCollection Servers { get; } = new ConnectorViewModelCollection();

    /// <summary>
    /// Gets the groups retained independently of the projects being edited.
    /// </summary>
    public IList<string> Groups { get; private set; } = new List<string>();

    /// <summary>
    /// Gets or sets the appearance preference.
    /// </summary>
    [OnChangedMethod(nameof(MarkModified))]
    public ApplicationTheme ThemeMode { get; set; }

    [OnChangedMethod(nameof(MarkModified))]
    public ApplicationLanguage Language { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a single new connector configuration mode is active.
    /// </summary>
    public bool SingleNewConnectorMode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether window should be shown in taskbar.
    /// </summary>
    [OnChangedMethod(nameof(MarkModified))]
    public bool ShowInTaskbar { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether unstable observation state is active.
    /// </summary>
    [OnChangedMethod(nameof(MarkModified))]
    public bool UnstableObservationState { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether failure observation state is active.
    /// </summary>
    [OnChangedMethod(nameof(MarkModified))]
    public bool FailureObservationState { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether success observation state is active.
    /// </summary>
    [OnChangedMethod(nameof(MarkModified))]
    public bool SuccessObservationState { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether running observation state is active.
    /// </summary>
    [OnChangedMethod(nameof(MarkModified))]
    public bool RunningObservationState { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether unknown observation state is active.
    /// </summary>
    [OnChangedMethod(nameof(MarkModified))]
    public bool UnknownObservationState { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to notify only if status was changed.
    /// </summary>
    [OnChangedMethod(nameof(MarkModified))]
    public bool NotifyOnlyIfStatusChanged { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether window should be always on top of other windows.
    /// </summary>
    [OnChangedMethod(nameof(MarkModified))]
    public bool AlwaysOnTop { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the window should open minimized.
    /// </summary>
    [OnChangedMethod(nameof(MarkModified))]
    public bool OpenMinimized { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether application should start with system.
    /// </summary>
    /// <value>
    /// <c>true</c> if true, start APP with the system ; otherwise, <c>false</c>.
    /// </value>
    [OnChangedMethod(nameof(MarkModified))]
    public bool OpenOnSystemStart
    {
      get
      {
        if (this.openOnSystemStart != null)
        {
          return this.openOnSystemStart.Value;
        }

        try
        {
          using (var startUpKey = Registry.CurrentUser.OpenSubKey(StartupSubKeyPath))
          {
            var startUpKeyValue = startUpKey?.GetValue(StartupKey);
            var startUpKeyValueString = startUpKeyValue as string;
            if (startUpKeyValueString == null)
            {
              return false;
            }

            if (!string.Equals(startUpKeyValueString.Trim('"'), Environment.ProcessPath, StringComparison.OrdinalIgnoreCase))
            {
              return false;
            }

            this.openOnSystemStart = true;
            return this.openOnSystemStart.Value;
          }
        }
        catch (Exception)
        {
          this.openOnSystemStart = false;
        }
        finally
        {
          if (this.openOnSystemStart == null)
          {
            this.openOnSystemStart = false;
          }
        }

        return this.openOnSystemStart.Value;
      }

      set => this.openOnSystemStart = value;
    }

    /// <summary>
    /// Gets or sets view style.
    /// </summary>
    [OnChangedMethod(nameof(MarkModified))]
    public int ViewStyle { get; set; }

    /// <summary>
    /// Gets or sets the initial focused configuration <see cref="ListBoxItem" />.
    /// </summary>
    public ListBoxItem? InitialFocusedConfigurationListBoxItem { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether single connector mode is active.
    /// </summary>
    public bool SingleConnectorMode { get; set; }

    /// <summary>
    /// Gets the original configuration.
    /// </summary>
    public ApplicationConfiguration Configuration { get; private set; } = new ApplicationConfiguration();

    /// <summary>
    /// Gets a value indicating whether configuration is modified.
    /// </summary>
    /// <value>
    /// <c>true</c> if configuration is modified; otherwise, <c>false</c>.
    /// </value>
    public bool ConfigurationIsModified { get; private set; }

    /// <summary>
    /// Gets a value indicating whether configuration is not modified.
    /// </summary>
    /// <value>
    /// <c>true</c> if configuration is not modified; otherwise, <c>false</c>.
    /// </value>
    public bool ConfigurationIsNotModified => !this.ConfigurationIsModified;

    /// <summary>
    /// Loads the configuration view model from the source object.
    /// </summary>
    /// <param name="configurationSource">The configuration source.</param>
    public void Load(ApplicationConfiguration configurationSource)
    {
      this.IsLoaded = false;
      try
      {
        this.Configuration = configurationSource;
        configurationSource.InitializeServerConfigurations();
        var serverSource = new ApplicationConfiguration();
        foreach (var server in configurationSource.ServerConfigurations)
        {
          serverSource.ConnectorsConfiguration.Add(new ConnectorConfiguration(
            server.Type,
            server.Name,
            server.ConfigurationItems.Select(item => new ConfigurationItem(item.Key, item.Value)).ToList()) { Identifier = server.Identifier });
        }

        this.Servers.CollectionItemPropertyChanged -= this.ConnectorsCollectionItemPropertyChanged;
        this.Servers.Load(serverSource);
        this.Servers.CollectionItemPropertyChanged += this.ConnectorsCollectionItemPropertyChanged;
        this.Groups = configurationSource.GetGroupNames().ToList();
        if (this.Connectors == null)
        {
          this.Connectors = new ConnectorViewModelCollection();
        }

        this.ThemeMode = configurationSource.ThemeMode;
        this.Language = configurationSource.Language;
        this.ShowInTaskbar = configurationSource.ShowInTaskbar;
        this.UnstableObservationState = configurationSource.NotificationConfiguration.UnstableNotificationEnabled;
        this.RunningObservationState = configurationSource.NotificationConfiguration.RunningNotificationEnabled;
        this.FailureObservationState = configurationSource.NotificationConfiguration.FailureNotificationEnabled;
        this.SuccessObservationState = configurationSource.NotificationConfiguration.SuccessNotificationEnabled;
        this.UnknownObservationState = configurationSource.NotificationConfiguration.UnknownNotificationEnabled;
        this.NotifyOnlyIfStatusChanged = configurationSource.NotificationConfiguration.OnlyIfChanged;
        this.AlwaysOnTop = configurationSource.AlwaysOnTop;
        this.OpenMinimized = configurationSource.OpenMinimized;
        this.ViewStyle = (int)configurationSource.ViewStyle;
        this.openOnSystemStart = null; // as this option is not really saved, we reset the private variable value.
        this.OnPropertyChanged(nameof(this.OpenOnSystemStart));

        this.ConfigurationIsModified = false;

        this.Connectors.CollectionChanged -= this.ConnectorsCollectionChanged;
        this.Connectors.CollectionItemPropertyChanged -= this.ConnectorsCollectionItemPropertyChanged;

        this.Connectors.Load(configurationSource);
      }
      finally
      {
        this.IsLoaded = true;
        if (this.Connectors != null)
        {
          this.Connectors.CollectionChanged += this.ConnectorsCollectionChanged;
          this.Connectors.CollectionItemPropertyChanged += this.ConnectorsCollectionItemPropertyChanged;
        }
      }
    }

    /// <summary>
    /// Applies to source configuration and saves changes.
    /// </summary>
    public async Task ApplyToSourceAndSaveAsync()
    {
      if (!this.ConfigurationIsModified)
      {
        return;
      }

      this.ConfigurationApplying?.Invoke(this, new EventArgs());
      try
      {
        this.Connectors.ApplyToConfiguration(this.Configuration);
        this.ApplyMainSettingsToConfiguration(this.Configuration);
        await SerializationHelper.Instance.SaveConfigurationAsync(this.Configuration);
        this.ConfigurationIsModified = false;
      }
      finally
      {
        this.ApplyRunWithWindowsOption();
        this.ConfigurationApplied?.Invoke(this, new ValueEventArgs<ApplicationConfiguration>(this.Configuration));
      }
    }

    /// <summary>
    /// Applies to configuration.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    public void ApplyToConfiguration(ApplicationConfiguration configuration)
    {
      foreach (var connector in this.Connectors)
      {
        var connectorConfiguration = connector.CreateNewConnectorConfiguration();
        configuration.ConnectorsConfiguration.Add(connectorConfiguration);
      }

      this.ApplyMainSettingsToConfiguration(configuration);
    }

    /// <summary>
    /// Exports the configuration to specified file path.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    public async Task ExportAsync(string filePath)
    {
      var tempConfig = new ApplicationConfiguration();
      this.ApplyToConfiguration(tempConfig);
      await SerializationHelper.Instance.SaveConfigurationAsync(tempConfig, filePath);
    }

    /// <summary>
    /// Imports the configuration from specified file.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns>True if import was successful.</returns>
    public async Task<(bool Success, string? ErrorMessage)> ImportAsync(string filePath)
    {
      try
      {
        var newConfiguration = await SerializationHelper.LoadAsync<ApplicationConfiguration>(filePath);
        this.ConfigurationApplying?.Invoke(this, new EventArgs());
        this.Load(newConfiguration);
        this.ConfigurationIsModified = true;
        await this.ApplyToSourceAndSaveAsync();
        this.ConfigurationIsModified = false;
        this.ConfigurationApplied?.Invoke(this, new ValueEventArgs<ApplicationConfiguration>(this.Configuration));
        return (true, null);
      }
      catch (Exception e)
      {
        var errorMessage = Localizer.ImportFailed(filePath, e.Message);
        log.Error(errorMessage);
        log.Error(e);
        return (false, errorMessage);
      }
    }

    /// <summary>
    /// Adds server editors for newly added projects, preserving unsaved credentials.
    /// </summary>
    public void RefreshServers()
    {
      var working = new ApplicationConfiguration();
      foreach (var server in this.Servers)
      {
        working.ServerConfigurations.Add(CreateServerConfiguration(server));
      }

      foreach (var project in this.Connectors.Where(item => item.SourceConnectorPlugin != null))
      {
        var snapshot = project.CreateNewConnectorConfiguration();
        snapshot.ServerConfiguration = project.SourceConnectorConfiguration?.ServerConfiguration;
        working.ConnectorsConfiguration.Add(snapshot);
      }

      working.InitializeServerConfigurations();
      foreach (var server in working.ServerConfigurations.Where(item => this.Servers.All(existing => existing.Identifier != item.Identifier)))
      {
        var model = new ConnectorViewModel();
        model.Load(server);
        this.Servers.Add(model);
      }
    }

    private static ConnectorConfiguration CreateServerConfiguration(ConnectorViewModel server) => new ConnectorConfiguration(
      server.SourceConnectorConfiguration?.Type ?? throw new InvalidOperationException("No server configuration is loaded."),
      server.Name,
      server.ConfigurationItems.Select(item => item.CreateNewConfigurationItem()).ToList()) { Identifier = server.Identifier };

    /// <summary>
    /// Called when significant property of the view model was changed.
    /// </summary>
    private void MarkModified()
    {
      if (this.IsLoaded)
      {
        this.ConfigurationIsModified = true;
      }
    }

    /// <summary>
    /// Applies the main settings to configuration.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    private void ApplyMainSettingsToConfiguration(ApplicationConfiguration configuration)
    {
      this.RefreshServers();
      configuration.ServerConfigurations.Clear();
      foreach (var server in this.Servers)
      {
        configuration.ServerConfigurations.Add(CreateServerConfiguration(server));
      }

      configuration.InitializeServerConfigurations();
      configuration.Groups.Clear();
      foreach (var group in this.Groups)
      {
        configuration.Groups.Add(group);
      }

      configuration.ThemeMode = this.ThemeMode;
      configuration.Language = this.Language;
      configuration.ShowInTaskbar = this.ShowInTaskbar;
      configuration.AlwaysOnTop = this.AlwaysOnTop;
      configuration.OpenMinimized = this.OpenMinimized;
      configuration.ViewStyle = (ViewStyle)this.ViewStyle;
      configuration.NotificationConfiguration.FailureNotificationEnabled = this.FailureObservationState;
      configuration.NotificationConfiguration.RunningNotificationEnabled = this.RunningObservationState;
      configuration.NotificationConfiguration.SuccessNotificationEnabled = this.SuccessObservationState;
      configuration.NotificationConfiguration.UnstableNotificationEnabled = this.UnstableObservationState;
      configuration.NotificationConfiguration.UnknownNotificationEnabled = this.UnknownObservationState;
      configuration.NotificationConfiguration.OnlyIfChanged = this.NotifyOnlyIfStatusChanged;
    }

    /// <summary>
    /// Handles the CollectionItemPropertyChanged event of the Connectors object.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.ComponentModel.PropertyChangedEventArgs" /> instance containing the event data.</param>
    private void ConnectorsCollectionItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
      this.MarkModified();
      this.OnPropertyChanged(nameof(this.Connectors));
    }

    /// <summary>
    /// Handles the changes of <see cref="Connectors" /> collection.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">
    /// The <see cref="System.Collections.Specialized.NotifyCollectionChangedEventArgs" /> instance containing
    /// the event data.
    /// </param>
    private void ConnectorsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => this.MarkModified();

    /// <summary>
    /// Applies the start with Windows option.
    /// </summary>
    private void ApplyRunWithWindowsOption()
    {
      try
      {
        using (var startUpKey = Registry.CurrentUser.OpenSubKey(StartupSubKeyPath, true))
        {
          if (startUpKey == null)
          {
            throw new InvalidOperationException(Strings.WindowsStartupKeyMissing);
          }

          var startUpKeyValue = startUpKey.GetValue(StartupKey);
          if (this.OpenOnSystemStart)
          {
            startUpKey.SetValue(StartupKey, $"\"{Environment.ProcessPath}\"");
          }
          else if (startUpKeyValue != null)
          {
            startUpKey.DeleteValue(StartupKey);
          }
        }
      }
      catch (Exception e)
      {
        // TODO log error
      }
    }
  }
}
