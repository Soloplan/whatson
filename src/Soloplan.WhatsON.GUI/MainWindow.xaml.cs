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
    private Configuration config;

    private ObservationScheduler scheduler;

    protected override void OnClosed(EventArgs e)
    {
      this.scheduler.Stop();
      base.OnClosed(e);
    }

    public MainWindow()
    {
      this.InitializeComponent();
      this.config = SerializationHelper.LoadConfiguration();
      this.scheduler = new ObservationScheduler();

      foreach (var subject in this.config.Subjects)
      {
        this.scheduler.Observe(subject);
      }

      this.mainTreeView.Init(this.scheduler, this.config);
      this.scheduler.Start();
    }

    private void OpenConfig(object sender, RoutedEventArgs e)
    {
      var configWindow = new ConfigWindow(this.config);
      configWindow.ShowDialog();
    }
  }
}
