// --------------------------------------------------------------------------------------------------------------------
// <copyright file="App.xaml.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI
{
  using System;
  using System.Globalization;
  using System.Linq;
  using System.Net;
  using System.Windows;
  using System.Windows.Interop;
  using System.Windows.Media.Animation;
  using NLog;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Logging;

  /// <summary>
  /// Interaction logic for App.xaml.
  /// </summary>
  public partial class App : Application
  {
    /// <summary>
    /// The argument name for the configuration directory.
    /// </summary>
    private const string ConfigDirArgName = @"/configDir:";

    /// <summary>
    /// Logger instance used by this class.
    /// </summary>
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// The theme helper.
    /// </summary>
    private readonly ThemeHelper themeHelper = new ThemeHelper();

    private ApplicationConfiguration config = new ApplicationConfiguration();

    private ObservationScheduler scheduler = new ObservationScheduler();

    private TrayHandler? handler;

    /// <summary>
    /// Gets a value indicating whether dark theme is enabled.
    /// </summary>
    /// <value>
    /// <c>true</c> if dark theme is enabled; otherwise, <c>false</c>.
    /// </value>
    public bool IsDarkThemeEnabled { get; private set; }

    public ApplicationTheme CurrentThemeMode { get; private set; } = ApplicationTheme.System;

    /// <summary>
    /// Applies the theme in a given mode.
    /// When the mode is not set, it is taken from the current configuration.
    /// </summary>
    /// <param name="mode">An optional appearance preference to preview.</param>
    public void ApplyTheme(ApplicationTheme? mode = null)
    {
      this.CurrentThemeMode = mode ?? this.config?.ThemeMode ?? ApplicationTheme.System;
      this.IsDarkThemeEnabled = ThemeHelper.IsDarkMode(this.CurrentThemeMode, ThemeHelper.SystemUsesDarkMode());
      this.themeHelper.ApplyLightDarkMode(this.IsDarkThemeEnabled);
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
      var configDirArg = e.Args.FirstOrDefault(a => a.ToLower().StartsWith(ConfigDirArgName.ToLower()));
      if (configDirArg != null)
      {
        configDirArg = configDirArg.Substring(ConfigDirArgName.Length);
        SerializationHelper.Instance.SetConfigFolder(configDirArg);
      }

      ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

      var logConfiguration = new LoggingConfiguration();
      logConfiguration.Initialize();
      ExceptionHandlingInitialization.Initialize();

      this.config = await SerializationHelper.Instance.LoadOrCreateConfigurationAsync();
      Localizer.ApplyLanguage(this.config.Language);
      FrameworkElement.LanguageProperty.OverrideMetadata(
        typeof(FrameworkElement),
        new FrameworkPropertyMetadata(System.Windows.Markup.XmlLanguage.GetLanguage(CultureInfo.CurrentUICulture.IetfLanguageTag)));
      this.scheduler = new ObservationScheduler();

      // call the plugins with the application args, so plugins can process them
      foreach (var connectorPlugin in PluginManager.Instance.ConnectorPlugins)
      {
        try
        {
          connectorPlugin.OnStartup(e.Args);
        }
        catch (Exception ex)
        {
          log.Error(ex);
        }
      }

      // add each connector to the observation scheduler
      foreach (var connectorConfiguration in this.config.ConnectorsConfiguration)
      {
        var connector = PluginManager.Instance.GetConnector(connectorConfiguration);
        this.scheduler.Observe(connector);
      }

      this.handler = new TrayHandler(this.scheduler, this.config);
      this.themeHelper.Initialize(this.handler.VisualSettings?.ColorSettings);
      this.ApplyTheme();
      Microsoft.Win32.SystemEvents.UserPreferenceChanged += this.SystemAppearanceChanged;

      if (!this.config.OpenMinimized)
      {
        this.handler.ShowOrHideWindow();
      }

      Timeline.DesiredFrameRateProperty.OverrideMetadata(typeof(Timeline), new FrameworkPropertyMetadata { DefaultValue = 15 });

      this.scheduler.Start();
      ComponentDispatcher.ThreadPreprocessMessage += this.ComponentDispatcherThreadPreprocessMessage;

      base.OnStartup(e);
    }

    /// <summary>
    /// Handle exiting application.
    /// </summary>
    /// <param name="e">Event args.</param>
    protected override void OnExit(ExitEventArgs e)
    {
      Microsoft.Win32.SystemEvents.UserPreferenceChanged -= this.SystemAppearanceChanged;
      this.scheduler.Stop(true);
      this.handler?.Dispose();
      base.OnExit(e);
    }

    private void SystemAppearanceChanged(object? sender, Microsoft.Win32.UserPreferenceChangedEventArgs e)
    {
      this.Dispatcher.InvokeAsync(() =>
      {
        if (this.CurrentThemeMode == ApplicationTheme.System)
        {
          this.ApplyTheme(ApplicationTheme.System);
        }
      });
    }

    private void ComponentDispatcherThreadPreprocessMessage(ref MSG msg, ref bool handled)
    {
      if (msg.message == 0x10)
      {
        Current.Shutdown();
      }
    }
  }
}
