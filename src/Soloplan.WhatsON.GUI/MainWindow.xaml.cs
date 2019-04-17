// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Soloplan GmbH">
//  Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI
{
  using System;
  using System.Collections.Generic;
  using System.Windows;
  using System.Windows.Input;
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
    public ApplicationConfiguration config { get; set; }

    /// <summary>
    /// The scheduler used for observing subjects.
    /// </summary>
    private ObservationScheduler scheduler;

    /// <summary>
    /// App settings.
    /// </summary>
    private MainWindowSettigns settings;

    /// <summary>
    /// Occurs when configuration was applied.
    /// </summary>
    public event EventHandler<ValueEventArgs<ApplicationConfiguration>> ConfigurationApplied;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="scheduler">The scheduler observing build servers.</param>
    /// <param name="configuration">App configuration.</param>
    /// <param name="initialSubjectState">The initial state of observed subjects.</param>
    public MainWindow(ObservationScheduler scheduler, ApplicationConfiguration configuration, IList<Subject> initialSubjectState)
    {
      this.InitializeComponent();
      this.scheduler = scheduler;
      this.config = configuration;
      this.mainTreeView.Init(this.scheduler, this.config, initialSubjectState);
      this.ShowInTaskbar = this.config.ShowInTaskbar;
      this.Topmost = this.config.AlwaysOnTop;
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
      else
      {
        this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
      }

      this.mainTreeView.ApplyTreeListSettings(this.settings.TreeListSettings);
      this.MinimizeButton.Visibility = this.ShowInTaskbar ? Visibility.Visible : Visibility.Hidden;
    }

    /// <summary>
    /// Applies configuration to main window.
    /// </summary>
    /// <param name="configuration">New configuration.</param>
    public void ApplyConfiguration(ApplicationConfiguration configuration)
    {
      this.config = configuration;
      this.mainTreeView.Update(this.config);
      this.ShowInTaskbar = this.config.ShowInTaskbar;
      this.Topmost = this.config.AlwaysOnTop;
      this.MinimizeButton.Visibility = this.ShowInTaskbar ? Visibility.Visible : Visibility.Hidden;
    }

    private void OpenConfig(object sender, RoutedEventArgs e)
    {
      var configWindow = new ConfigWindow(this.config);
      configWindow.Owner = this;
      configWindow.ConfigurationApplied += (s, ev) =>
      {
        this.ConfigurationApplied?.Invoke(this, ev);
      };

      if (this.settings.ConfigDialogSettings != null)
      {
        this.settings.ConfigDialogSettings.Apply(configWindow);
      }
      else
      {
        configWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
      }

      configWindow.Closing += (s, ev) =>
      {
        this.settings.ConfigDialogSettings = new WindowSettings().Parse(configWindow);
      };

      configWindow.ShowDialog();
    }

    /// <summary>
    /// Main window bar mouse down.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
    private void MainWindowBarMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      if (e.ChangedButton != MouseButton.Left || e.Handled || e.ButtonState == MouseButtonState.Released)
      {
        return;
      }

      if (e.ClickCount == 2)
      {
        this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
      }
      else
      {
        this.DragMove();
      }
    }

    /// <summary>
    /// Minimizes the mouse down.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="MouseButtonEventArgs"/> instance containing the event data.</param>
    private void MinimizeButonClick(object sender, RoutedEventArgs e)
    {
      this.WindowState = WindowState.Minimized;
    }

    /// <summary>
    /// Closes the mouse down.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
    private void CloseButtonClick(object sender, RoutedEventArgs e)
    {
      this.Close();
    }
  }
}
