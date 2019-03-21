// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Soloplan GmbH">
//  Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI
{
  using System;
  using System.ComponentModel;
  using System.Windows;
  using Soloplan.WhatsON.GUI.Config.View;
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

    private bool shown;

    protected override void OnClosed(EventArgs e)
    {
      this.scheduler.Stop();
      base.OnClosed(e);
    }

    public MainWindow()
    {
      this.InitializeComponent();
      this.config = SerializationHelper.LoadOrCreateConfiguration();
      this.scheduler = new ObservationScheduler();

      foreach (var subjectConfiguration in this.config.SubjectsConfiguration)
      {
        var subject = PluginsManager.Instance.GetSubject(subjectConfiguration);
        this.scheduler.Observe(subject);
      }

      this.mainTreeView.Init(this.scheduler, this.config);

      var themeHelper = new ThemeHelper();
      themeHelper.Initialize();
      themeHelper.ApplyLightDarkMode(this.config.DarkThemeEnabled);
    }

    private void OpenConfig(object sender, RoutedEventArgs e)
    {
      var configWindow = new ConfigWindow(this.config);
      configWindow.ConfigurationApplied += (s, ev) =>
      {
        this.config = ev.Value;

        this.ApplyConfiguration();
      };

      configWindow.ShowDialog();
    }

    protected override void OnContentRendered(EventArgs e)
    {
      base.OnContentRendered(e);

      if (this.shown)
      {
        return;
      }

      this.scheduler.Start();
      this.shown = true;
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
    }

    private void StopObservation(object sender, RoutedEventArgs e)
    {
      this.scheduler.Stop();
    }

    private void StartObservation(object sender, RoutedEventArgs e)
    {
      this.scheduler.Start();
    }
  }
}
