namespace Soloplan.WhatsON.GUI
{
  using System.Net;
  using System.Windows;
  using System.Windows.Interop;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common.VisualConfig;
  using Soloplan.WhatsON.GUI.Logging;
  using Soloplan.WhatsON.Serialization;

  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : Application
  {
    /// <summary>
    /// The theme helper.
    /// </summary>
    private readonly ThemeHelper themeHelper = new ThemeHelper();

    private ApplicationConfiguration config;

    private ObservationScheduler scheduler;

    private TrayHandler handler;

    /// <summary>
    /// Gets a value indicating whether dark theme is enabled.
    /// </summary>
    /// <value>
    ///   <c>true</c> if dark theme is enabled; otherwise, <c>false</c>.
    /// </value>
    public bool IsDarkThemeEnabled { get; private set; }

    /// <summary>
    /// Applies the theme in a given mode.
    /// When the mode is not set, it is taken from the current configuration.
    /// </summary>
    /// <param name="isDark">The is dark.</param>
    public void ApplyTheme(bool? isDark = null)
    {
      if (isDark == null)
      {
        isDark = this.config.DarkThemeEnabled;
      }

      this.IsDarkThemeEnabled = isDark.Value;
      this.themeHelper.ApplyLightDarkMode(isDark.Value);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);

      ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

      var logConfiguration = new LoggingConfiguration();
      logConfiguration.Initialize();
      ExceptionHandlingInitialization.Initialize();

      this.config = SerializationHelper.LoadOrCreateConfiguration();
      this.scheduler = new ObservationScheduler();

      foreach (var connectorConfiguration in this.config.ConnectorsConfiguration)
      {
        var connector = PluginsManager.Instance.GetConnector(connectorConfiguration);
        this.scheduler.Observe(connector);
      }

      this.handler = new TrayHandler(this.scheduler, this.config);
      this.themeHelper.Initialize(this.handler.VisualSettings?.ColorSettings);
      this.ApplyTheme();

      this.scheduler.Start();
      ComponentDispatcher.ThreadPreprocessMessage += this.ComponentDispatcherThreadPreprocessMessage;
    }

    private void ComponentDispatcherThreadPreprocessMessage(ref MSG msg, ref bool handled)
    {
      if (msg.message == 0x10)
      {
        System.Windows.Application.Current.Shutdown();
      }
    }

    /// <summary>
    /// Handle exiting application.
    /// </summary>
    /// <param name="e">Event args.</param>
    protected override void OnExit(ExitEventArgs e)
    {
      this.scheduler.Stop(true);
      this.handler.Dispose();
      base.OnExit(e);
    }
  }
}
