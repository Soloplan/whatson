// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Soloplan GmbH">
//  Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI
{
  using System.Collections.Generic;
  using System.Windows;
  using Soloplan.WhatsON.GUI.Config.View;
  using Soloplan.WhatsON.GUI.VisualConfig;
  using Soloplan.WhatsON.Serialization;

  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow
  {
    /// <summary>
    /// The configuration.
    /// </summary>
    private ApplicationConfiguration config;

    private ObservationScheduler scheduler;

    private MainWindowSettigns settings;

    public MainWindow(ObservationScheduler scheduler, ApplicationConfiguration configuration, IList<Subject> initialSubjectState)
    {
      this.InitializeComponent();
      this.scheduler = scheduler;
      this.config = configuration;
      this.mainTreeView.Init(this.scheduler, this.config, initialSubjectState);
      this.ShowInTaskbar = this.config.ShowInTaskbar;
    }

    public MainWindowSettigns GetVisualSettigns()
    {
      this.settings.TreeListSettings = this.mainTreeView.GetTreeListSettings();
      this.settings.MainWindowDimensions = new WindowSettings().Parse(this);

      return this.settings;
    }

    public void ApplyVisualSettings(MainWindowSettigns visualSettings)
    {
      this.settings = visualSettings ?? new MainWindowSettigns();

      if (this.settings.MainWindowDimensions != null)
      {
        this.settings.MainWindowDimensions.Apply(this);
      }

      this.mainTreeView.ApplyTreeListSettings(this.settings.TreeListSettings);
    }

    private void OpenConfig(object sender, RoutedEventArgs e)
    {
      var configWindow = new ConfigWindow(this.config);
      configWindow.Owner = this;
      configWindow.ConfigurationApplied += (s, ev) =>
      {
        this.config = ev.Value;

        this.ApplyConfiguration();
      };

      if (this.settings.ConfigDialogSettings != null)
      {
        this.settings.ConfigDialogSettings.Apply(configWindow);
      }

      configWindow.Closing += (s, ev) =>
      {
        this.settings.ConfigDialogSettings = new WindowSettings().Parse(configWindow);
      };

      configWindow.ShowDialog();
    }

    private void ApplyConfiguration()
    {
      var schedulerRunning = this.scheduler.Running;
      if (this.scheduler.Running)
      {
        this.scheduler.Stop();
      }

      this.scheduler.UnobserveAll();
      foreach (var subjectConfiguration in this.config.SubjectsConfiguration)
      {
        var subject = PluginsManager.Instance.GetSubject(subjectConfiguration);
        this.scheduler.Observe(subject);
      }

      this.mainTreeView.Update(this.config);

      if (schedulerRunning)
      {
        this.scheduler.Start();
      }

      this.ShowInTaskbar = this.config.ShowInTaskbar;
    }
  }
}
