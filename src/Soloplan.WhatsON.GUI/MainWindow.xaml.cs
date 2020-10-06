// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI
{
  using System;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Linq;
  using System.Threading.Tasks;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Media.Animation;
  using MaterialDesignThemes.Wpf;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.GUI.Common.VisualConfig;
  using Soloplan.WhatsON.GUI.Configuration;
  using Soloplan.WhatsON.GUI.Configuration.View;
  using Soloplan.WhatsON.GUI.Configuration.ViewModel;
  using Soloplan.WhatsON.GUI.Configuration.Wizard;
  using Soloplan.WhatsON.Model;

  /// <summary>
  /// Interaction logic for MainWindow.xaml.
  /// </summary>
  public partial class MainWindow : INotifyPropertyChanged
  {
    public const string VisualSettingsFile = "visualsettings.json";

    private readonly IList<Connector> initialConnectorState;

    /// <summary>
    /// Gets or sets the configuration.
    /// </summary>
    private ApplicationConfiguration config;

    /// <summary>
    /// The scheduler used for observing connectors.
    /// </summary>
    private ObservationScheduler scheduler;

    /// <summary>
    /// App settings.
    /// </summary>
    private MainWindowSettings settings;

    private bool initialized;

    /// <summary>
    /// Backing field for <see cref="ConfigurationModifiedFromTree"/>.
    /// </summary>
    private bool configurationModifiedFromTree;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="scheduler">The scheduler observing build servers.</param>
    /// <param name="configuration">App configuration.</param>
    /// <param name="initialConnectorState">The initial state of observed connectors.</param>
    public MainWindow(ObservationScheduler scheduler, ApplicationConfiguration configuration, IList<Connector> initialConnectorState)
    {
      this.InitializeComponent();
      this.scheduler = scheduler;
      this.config = configuration;
      this.initialConnectorState = initialConnectorState;
      this.ShowInTaskbar = this.config.ShowInTaskbar;
      this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.ShowInTaskbar)));
      this.Topmost = this.config.AlwaysOnTop;
      this.DataContext = this;
      this.mainTreeView.ConfigurationChanged += this.MainTreeViewOnConfigurationChanged;
      this.mainTreeView.EditItem += this.EditTreeItem;
      this.mainTreeView.DeleteItem += this.OnItemDeleted;
      this.mainTreeView.ExportItem += this.ExportTreeItem;
    }

    /// <summary>
    /// Occurs when configuration was applied.
    /// </summary>
    public event EventHandler<ValueEventArgs<ApplicationConfiguration>> ConfigurationApplied;

    public event PropertyChangedEventHandler PropertyChanged;

    public bool IsTreeInitialized
    {
      get => this.initialized;
      set
      {
        this.initialized = value;
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsTreeInitialized)));
      }
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

    public MainWindowSettings GetVisualSettings()
    {
      this.settings.TreeListSettings = this.mainTreeView.GetTreeListSettings();
      this.settings.Maximized = this.WindowState == WindowState.Maximized;
      if (!this.settings.Maximized)
      {
        this.settings.MainWindowDimensions = new WindowSettings().Parse(this);
        this.settings.Left = this.Left;
        this.settings.Top = this.Top;
      }

      this.settings.ColorSettings = new ColorSettings();
      this.settings.ColorSettings.Primary.Apply(ThemeHelper.PrimaryColor);
      this.settings.ColorSettings.Secondary.Apply(ThemeHelper.SecondaryColor);

      return this.settings;
    }

    public void ApplyVisualSettings(MainWindowSettings visualSettings)
    {
      this.settings = visualSettings ?? new MainWindowSettings();

      if (this.settings.MainWindowDimensions != null)
      {
        this.settings.MainWindowDimensions.Apply(this);
        this.Top = this.settings.Top;
        this.Left = this.settings.Left;
        this.WindowState = this.settings.Maximized ? WindowState.Maximized : WindowState.Normal;
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
      this.mainTreeView.Init(this.scheduler, this.config, this.initialConnectorState);
      this.mainTreeView.ApplyTreeListSettings(this.settings.TreeListSettings);
    }

    /// <summary>Raises the <see cref="E:System.Windows.Window.Closed" /> event. Performs necessry cleanup.</summary>
    /// <param name="e">An <see cref="T:System.EventArgs" /> that contains the event data.</param>
    protected override void OnClosed(EventArgs e)
    {
      this.mainTreeView.ConfigurationChanged -= this.MainTreeViewOnConfigurationChanged;
      this.mainTreeView.EditItem -= this.EditTreeItem;
      this.mainTreeView.DeleteItem -= this.OnItemDeleted;
      this.mainTreeView.ExportItem -= this.ExportTreeItem;
      base.OnClosed(e);
      this.mainTreeView.Dispose();
      this.mainTreeView = null;
    }

    private void OpenConfig(object sender, EventArgs e)
    {
      var configWindow = new ConfigWindow(this.config, this.settings.WizardDialogSettings);
      this.OpenConfig(configWindow);
    }

    /// <summary>
    /// Handles the Click event of the add new connector button control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    private void NewConnectorClick(object sender, EventArgs e)
    {
      var wizardController = new WizardController(this, this.config, this.settings.WizardDialogSettings);
      var result = false;
      try
      {
        result = wizardController.Start();
      }
      finally
      {
        if (result)
        {
          this.ConfigurationApplied?.Invoke(this, new ValueEventArgs<ApplicationConfiguration>(this.config));
        }
      }
    }

    /// <summary>
    /// Handles the import click.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    private void ImportClick(object sender, EventArgs e)
    {
      var interchange = new ProjectsDataInterchange();
      if (interchange.Import(this.config))
      {
        this.ConfigurationApplied?.Invoke(this, new ValueEventArgs<ApplicationConfiguration>(this.config));
      }
    }

    private void MainTreeViewOnConfigurationChanged(object sender, EventArgs e)
    {
      var showAnimation = !this.ConfigurationModifiedFromTree;
      this.ConfigurationModifiedFromTree = true;
      if (showAnimation)
      {
        if (this.FindResource("showStoryBoard") is Storyboard sb)
        {
          this.BeginStoryboard(sb);
        }
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
      SerializationHelper.Instance.SaveConfiguration(this.config);
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

    /// <summary>
    /// Handles creation of new group.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">Event args.</param>
    private async void NewGroupClick(object sender, EventArgs e)
    {
      var model = new GroupViewModel
      {
        AlreadyUsedNames = this.mainTreeView.GetGroupNames(),
      };

      var editGroupDialog = new EditGroupNameDialog(model);
      var result = await this.ShowDialogOnPageHost(editGroupDialog);
      if (result)
      {
        this.mainTreeView.CreateGroup(model.Name);
      }
    }

    /// <summary>
    /// Opens the configuration.
    /// </summary>
    private void OpenConfig(ConfigWindow configWindow)
    {
      if (this.ConfigurationModifiedFromTree)
      {
        return;
      }

      configWindow.Owner = this;
      configWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
      configWindow.ConfigurationApplied += (s, ev) =>
      {
        this.ConfigurationApplied?.Invoke(this, ev);
      };

      this.settings.ConfigDialogSettings?.Apply(configWindow);

      configWindow.Closing += (s, ev) =>
      {
        this.settings.ConfigDialogSettings = new WindowSettings().Parse(configWindow);
      };

      configWindow.ShowDialog();
    }

    /// <summary>
    /// Opens the configuration.
    /// </summary>
    /// <param name="connector">The connector.</param>
    private void OpenConfig(Connector connector)
    {
      var configWindow = new ConfigWindow(this.config, connector, this.settings.WizardDialogSettings);
      this.OpenConfig(configWindow);
    }

    /// <summary>
    /// Handles editing of tree item.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">Event args.</param>
    private void ExportTreeItem(object sender, ValueEventArgs<Common.ConnectorTreeView.TreeItemViewModel> e)
    {
      if (e.Value is ConnectorGroupViewModel groupTreeViewModel)
      {
        var connectors = new List<ConnectorConfiguration>();
        foreach (var connectorViewModelItem in groupTreeViewModel.ConnectorViewModels)
        {
          connectors.Add(connectorViewModelItem.Connector.Configuration);
        }

        var projectsDataInterchange = new ProjectsDataInterchange();
        projectsDataInterchange.Export(connectors, groupTreeViewModel.GroupName);
      }

      if (e.Value is Common.ConnectorTreeView.ConnectorViewModel connectorViewModel)
      {
        var projectsDataInterchange = new ProjectsDataInterchange();
        projectsDataInterchange.Export(connectorViewModel.Connector.Configuration, connectorViewModel.Name);
      }
    }

    /// <summary>
    /// Handles editing of tree item.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">Event args.</param>
    private async void EditTreeItem(object sender, Common.ConnectorTreeView.EditTreeItemViewModelEventArgs e)
    {
      if (e.Model is ConnectorGroupViewModel groupTreeViewModel)
      {
        var model = new GroupViewModel
        {
          Name = groupTreeViewModel.GroupName,
          AlreadyUsedNames = this.mainTreeView.GetGroupNames().Where(grpName => grpName != groupTreeViewModel.GroupName).ToList(),
        };
        var editGroupDialog = new EditGroupNameDialog(model);
        var result = await this.ShowDialogOnPageHost(editGroupDialog);
        if (result && groupTreeViewModel.GroupName != model.Name)
        {
          groupTreeViewModel.GroupName = model.Name;
        }
      }

      if (e.Model is Common.ConnectorTreeView.ConnectorViewModel connectorViewModel)
      {
        if (e.EditType == EditType.Rename)
        {
          var model = new Soloplan.WhatsON.GUI.Configuration.ViewModel.ConnectorViewModel
          {
            Name = connectorViewModel.Name,
          };

          var editProjectNameDialog = new EditProjectName(model);
          var result = await this.ShowDialogOnPageHost(editProjectNameDialog);
          if (result && connectorViewModel.Name != model.Name)
          {
            var connector = this.config.ConnectorsConfiguration.FirstOrDefault(c => c.Identifier == connectorViewModel.Identifier);
            if (connector != null)
            {
              connectorViewModel.Name = model.Name;
            }
          }
        }
        else
        {
          this.OpenConfig(connectorViewModel.Connector);
        }
      }
    }

    /// <summary>
    /// Called for confirming item deletion.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">Event args containing object which should be deleted.</param>
    private void OnItemDeleted(object sender, DeleteTreeItemEventArgs e)
    {
      string message;
      if (e.DeleteItem is ConnectorGroupViewModel group)
      {
        message = $"Are you sure you want to delete the group '{group.GroupName}' and all its items?";
      }
      else if (e.NoOtherSelections == false && e.DeleteItem is Soloplan.WhatsON.GUI.Common.ConnectorTreeView.ConnectorViewModel)
      {
        message = $"Are you sure you want to delete multiple connectors?";
      }
      else if (e.DeleteItem is Soloplan.WhatsON.GUI.Common.ConnectorTreeView.ConnectorViewModel connector)
      {
        message = $"Are you sure you want to delete the project '{connector.Name}'?";
      }
      else
      {
        message = $"Are you sure you want to delete the project '{e.DeleteItem}'";
      }

      var dialog = new OkCancelDialog(message);
      e.AddAsyncCancelCheckAction(async () => !await this.ShowDialogOnPageHost(dialog));
    }

    /// <summary>
    /// Shows <paramref name="dialog"/> in DialogHost "MainWindowPageHost". Works only with dialogs returning true/false.
    /// </summary>
    /// <param name="dialog">Dialog to show.</param>
    /// <returns>True or false depending on what option user selected.</returns>
    private async Task<bool> ShowDialogOnPageHost(UserControl dialog)
    {
      var tmpResult = await DialogHost.Show(dialog, "MainWindowPageHost");
      if (tmpResult is bool result)
      {
        return result;
      }

      return false;
    }
  }
}
