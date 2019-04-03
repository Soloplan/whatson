namespace Soloplan.WhatsON.GUI
{
  using System.Windows;
  using System.Windows.Interop;
  using Soloplan.WhatsON.Serialization;

  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : Application
  {
    private ApplicationConfiguration config;

    private ObservationScheduler scheduler;

    private TrayHandler handler;

    protected override void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);

      var logConfiguration = new LoggingConfiguration();
      logConfiguration.Initialize();
      ExceptionHandlingInitialization.Initialize();

      this.config = SerializationHelper.LoadOrCreateConfiguration();
      this.scheduler = new ObservationScheduler();

      foreach (var subjectConfiguration in this.config.SubjectsConfiguration)
      {
        var subject = PluginsManager.Instance.GetSubject(subjectConfiguration);
        this.scheduler.Observe(subject);
      }

      var themeHelper = new ThemeHelper();
      themeHelper.Initialize();
      themeHelper.ApplyLightDarkMode(this.config.DarkThemeEnabled);

      this.handler = new TrayHandler(this.scheduler, this.config);
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
      this.scheduler.Stop();
      this.handler.Dispose();
      base.OnExit(e);
    }
  }
}
