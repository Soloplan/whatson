namespace Soloplan.WhatsON.GUI
{
  using System.Windows;
  using Soloplan.WhatsON.GUI.SubjectTreeView;
  using Soloplan.WhatsON.Serialization;

  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : Application
  {
    private ApplicationConfiguration config;

    private ObservationScheduler scheduler;

    private SubjectTreeViewModel model;

    private TrayHandler handler;

    protected override void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);

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

      this.model = new SubjectTreeViewModel();
      this.model.Init(this.scheduler, this.config);
      foreach (var subjectGroupViewModel in this.model.SubjectGroups)
      {
        foreach (var subjectViewModel in subjectGroupViewModel.SubjectViewModels)
        {
          subjectViewModel.CurrentStatus.PropertyChanged += this.CurrentStatusPropertyChanged;
        }
      }

      this.handler = new TrayHandler(this.scheduler, this.config);
      this.scheduler.Start();
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

    private void CurrentStatusPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
      if (sender is StatusViewModel statusViewModel && e.PropertyName == nameof(StatusViewModel.State))
      {
        var description = $"Project name: {statusViewModel.Parent.Name}.";
        if (statusViewModel.State == ObservationState.Running)
        {
          this.handler.ShowBaloon("Build started.", description, System.Windows.Forms.ToolTipIcon.None);
        }
        else if (statusViewModel.State == ObservationState.Failure)
        {
          this.handler.ShowBaloon("Build Failed.", description, System.Windows.Forms.ToolTipIcon.Error);
        }
        else if (statusViewModel.State == ObservationState.Success)
        {
          this.handler.ShowBaloon("Build succeed", description, System.Windows.Forms.ToolTipIcon.Info);
        }
        else if (statusViewModel.State == ObservationState.Unstable)
        {
          this.handler.ShowBaloon("Build succeed (Unstable)", description, System.Windows.Forms.ToolTipIcon.Warning);
        }
        else if (statusViewModel.State == ObservationState.Unknown)
        {
          this.handler.ShowBaloon("Build interrupted", description, System.Windows.Forms.ToolTipIcon.Warning);
        }
      }
    }
  }
}
