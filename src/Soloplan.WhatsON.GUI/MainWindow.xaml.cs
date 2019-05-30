// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Soloplan GmbH">
//  Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI
{
  using System;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Windows;
  using System.Windows.Media.Animation;
  using Soloplan.WhatsON.GUI.Common.VisualConfig;
  using Soloplan.WhatsON.GUI.Config.View;
  using Soloplan.WhatsON.GUI.Config.Wizard;
  using Soloplan.WhatsON.Serialization;

  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : INotifyPropertyChanged
  {
    private readonly IList<Subject> initialSubjectState;

    /// <summary>
    /// Gets or sets the configuration.
    /// </summary>
    private ApplicationConfiguration config;

    /// <summary>
    /// The scheduler used for observing subjects.
    /// </summary>
    private ObservationScheduler scheduler;

    /// <summary>
    /// App settings.
    /// </summary>
    private MainWindowSettigns settings;

    private bool initialized;

    /// <summary>
    /// Backing field for <see cref="ConfigurationModifiedFromTree"/>.
    /// </summary>
    private bool configurationModifiedFromTree;

    /// <summary>
    /// Occurs when configuration was applied.
    /// </summary>
    public event EventHandler<ValueEventArgs<ApplicationConfiguration>> ConfigurationApplied;

    public event PropertyChangedEventHandler PropertyChanged;

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
      this.initialSubjectState = initialSubjectState;
      this.ShowInTaskbar = this.config.ShowInTaskbar;
      this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.ShowInTaskbar)));
      this.Topmost = this.config.AlwaysOnTop;
      this.DataContext = this;
      this.mainTreeView.ConfigurationChanged += this.MainTreeViewOnConfigurationChanged;
    }

    public bool IsTreeInitialized
    {
      get => this.initialized;
      set
      {
        this.initialized = value;
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsTreeInitialized)));
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsTreeNotInitialized)));
      }
    }

    public bool IsTreeNotInitialized
    {
      get => !this.IsTreeInitialized;
    }

    /// <summary>
    /// Gets or sets a value indicating whether configuration was modified from tree view.
    /// </summary>
    public bool ConfigurationModifiedFromTree
    {
      get => this.configurationModifiedFromTree;
      set
      {
        this.configurationModifiedFromTree = value;
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.ConfigurationModifiedFromTree)));
      }
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
      this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.ShowInTaskbar)));
      this.Topmost = this.config.AlwaysOnTop;
    }

    public void FinishDrawing()
    {
      this.mainTreeView.Init(this.scheduler, this.config, this.initialSubjectState);
      this.mainTreeView.ApplyTreeListSettings(this.settings.TreeListSettings);
    }

    /// <summary>
    /// Focuses the node connected with <paramref name="subject"/>.
    /// </summary>
    /// <param name="subject">Subject which should be focused.</param>
    public void FocusSubject(Subject subject)
    {
      this.mainTreeView.FocusItem(subject);
    }

    private void OpenConfig(object sender, EventArgs e)
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
    /// Handles the Click event of the add new connector button control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
    private void NewConnectorClick(object sender, RoutedEventArgs e)
    {
      var wizardController = new WizardController(this);
      var result = false;
      try
      {
        result = wizardController.Start(this.config);
      }
      finally
      {
        if (result)
        {
          this.ConfigurationApplied?.Invoke(this, new ValueEventArgs<ApplicationConfiguration>(this.config));
        }
      }
    }

    private void MainTreeViewOnConfigurationChanged(object sender, EventArgs e)
    {
      this.ConfigurationModifiedFromTree = true;
      if (this.FindResource("showStoryBoard") is Storyboard sb)
      {
        this.BeginStoryboard(sb);
      }
    }

    private void ResetClick(object sender, RoutedEventArgs e)
    {
      this.HideChangesPanel();
      this.ConfigurationApplied?.Invoke(this, new ValueEventArgs<ApplicationConfiguration>(this.config));
    }

    private void SaveClick(object sender, RoutedEventArgs e)
    {
      this.HideChangesPanel();

      this.mainTreeView.WriteToConfiguration(this.config);
      SerializationHelper.SaveConfiguration(this.config);
      this.ConfigurationApplied?.Invoke(this, new ValueEventArgs<ApplicationConfiguration>(this.config));
    }

    private void HideChangesPanel()
    {
      if (this.FindResource("hideStorBoard") is Storyboard sb)
      {
        void ChangePanelVisibility(object sender, EventArgs eventArgs)
        {
          this.ConfigurationModifiedFromTree = false;
          sb.Completed -= ChangePanelVisibility;
        }

        sb.Completed += ChangePanelVisibility;

        this.BeginStoryboard(sb);
      }
      else
      {
        this.ConfigurationModifiedFromTree = false;
      }
    }
  }
}
