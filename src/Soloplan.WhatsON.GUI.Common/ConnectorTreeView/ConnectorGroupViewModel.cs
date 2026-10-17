// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConnectorGroupViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.ConnectorTreeView
{
  using System;
  using System.Collections.Generic;
  using System.Collections.ObjectModel;
  using System.Linq;
  using System.Windows;
  using System.Windows.Input;
  using NLog;
  using PropertyChanged;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Model;

  /// <summary>
  /// Viewmodel representing group of connectors shown as single node in <see cref="ConnectorTreeView" />.
  /// </summary>
  public class ConnectorGroupViewModel : TreeItemViewModel
  {
    /// <summary>
    /// The logger.
    /// </summary>
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    private ObservableCollection<ConnectorViewModel>? statusViewModels;

    public ConnectorGroupViewModel()
    {
      this.IsNodeExpanded = true;
    }

    public ObservableCollection<ConnectorViewModel> ConnectorViewModels =>
      this.statusViewModels ?? (this.statusViewModels = new ObservableCollection<ConnectorViewModel>());

    [OnChangedMethod(nameof(GroupNameChanged))]
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether configuration was modified from main window.
    /// </summary>
    public override bool ConfigurationModifiedInTree
    {
      get => base.ConfigurationModifiedInTree;
      set
      {
        base.ConfigurationModifiedInTree = value;
        foreach (var connector in this.ConnectorViewModels)
        {
          connector.ConfigurationModifiedInTree = value;
        }
      }
    }

    /// <summary>
    /// Gets the visibility state of column for <see cref="ObservationState.Running" />.
    /// </summary>
    public Visibility RunningConnectorColumnVisibility { get; private set; } = Visibility.Collapsed;

    /// <summary>
    /// Gets the number of running connectors.
    /// </summary>
    [OnChangedMethod(nameof(SetRunningColumnVisible))]
    public int RunningConnectors { get; private set; } = -1;

    /// <summary>
    /// Gets the visibility state of column for <see cref="ObservationState.Failure" />.
    /// </summary>
    public Visibility FailureConnectorColumnVisibility { get; private set; } = Visibility.Collapsed;

    /// <summary>
    /// Gets the number of failing connectors.
    /// </summary>
    [OnChangedMethod(nameof(SetFailureColumnVisible))]
    public int FailingConnectors { get; private set; } = -1;

    /// <summary>
    /// Gets the visibility state of column for <see cref="ObservationState.Unstable" />.
    /// </summary>
    public Visibility UnstableConnectorColumnVisibility { get; private set; } = Visibility.Collapsed;

    /// <summary>
    /// Gets the number of unstable connectors.
    /// </summary>
    [OnChangedMethod(nameof(SetUnstableColumnVisible))]
    public int UnstableConnectors { get; private set; } = -1;

    /// <summary>
    /// Gets the visibility state of column for <see cref="ObservationState.Unknown" />.
    /// </summary>
    public Visibility UnknownConnectorColumnVisibility { get; private set; } = Visibility.Collapsed;

    /// <summary>
    /// Gets the number of connectors in unknown state.
    /// </summary>
    [OnChangedMethod(nameof(SetUnknownColumnVisible))]
    public int UnknownConnectors { get; private set; } = -1;

    /// <summary>
    /// Called when connector changes, ex new status.
    /// </summary>
    /// <param name="changedConnector">The connector which changed.</param>
    /// <returns>True if something was changed; false otherwise.</returns>
    public bool Update(Connector changedConnector)
    {
      var changedViewModel = this.ConnectorViewModels.FirstOrDefault(connector => connector.Identifier == changedConnector.Configuration.Identifier);
      if (changedViewModel != null)
      {
        changedViewModel.Update(changedConnector);
        this.UpdateConnectorWithGivenStatusCount();
        return true;
      }

      return false;
    }

    /// <summary>
    /// Should be called when configuration changed.
    /// </summary>
    /// <param name="connectorGroup">Grouping of connectors by group name.</param>
    public void Init(IGrouping<string, ConnectorConfiguration> connectorGroup) => this.Init(connectorGroup.Key ?? string.Empty, connectorGroup);

    /// <summary>
    /// Initializes a group, including a named group without projects.
    /// </summary>
    /// <param name="groupName">The group name.</param>
    /// <param name="connectorGroup">The projects belonging to the group.</param>
    public void Init(string groupName, IEnumerable<ConnectorConfiguration> connectorGroup)
    {
      this.ConnectorViewModels.CollectionChanged -= this.ConnectorViewModelsCollectionChanged;
      this.GroupName = groupName;
      log.Debug("Initializing group {GroupName}", this.GroupName);

      var connectorsNoLongerPresent = this.ConnectorViewModels
        .Where(model => connectorGroup.All(configurationSubject => configurationSubject.Identifier != model.Identifier)).ToList();
      var newConnectors = connectorGroup.Where(configurationSubject =>
        this.ConnectorViewModels.All(viewModel => configurationSubject.Identifier != viewModel.Identifier));
      foreach (var noLongerPresentConnectorViewModel in connectorsNoLongerPresent)
      {
        log.Debug(
          "Remove no longer present connector {noLongerPresentConnectorViewModel}",
          new { noLongerPresentConnectorViewModel.Identifier, noLongerPresentConnectorViewModel.Name });
        noLongerPresentConnectorViewModel.EditItem -= this.OnSubItemEdit;
        noLongerPresentConnectorViewModel.DeleteItem -= this.DeleteConnector;
        noLongerPresentConnectorViewModel.ExportItem -= this.ExportItemHandler;
        noLongerPresentConnectorViewModel.ConfigurationChanged -= this.ConnectorViewModelConfigurationChanged;
        this.ConnectorViewModels.Remove(noLongerPresentConnectorViewModel);
      }

      var addedIds = new List<Guid>();
      foreach (var newConnector in newConnectors)
      {
        log.Debug("Adding new connector {connectorConfiguration}", new { newConnector.Identifier, newConnector.Name });
        addedIds.Add(newConnector.Identifier);
        this.CreateViewModelForConnectorConfiguration(newConnector);
      }

      int index = 0;
      foreach (var config in connectorGroup)
      {
        log.Debug("Updating viewmodel for {connectorConfiguration}", new { config.Identifier, config.Name });
        var connectorViewModel = this.ConnectorViewModels.FirstOrDefault(model => model.Identifier == config.Identifier);
        if (connectorViewModel == null)
        {
          continue;
        }

        var oldIndex = this.ConnectorViewModels.IndexOf(connectorViewModel);
        if (oldIndex != index)
        {
          this.ConnectorViewModels.Move(oldIndex, index);
        }

        index++;
      }

      this.ConnectorViewModels.CollectionChanged += this.ConnectorViewModelsCollectionChanged;
    }

    public override void OnDoubleClick(object? sender, MouseButtonEventArgs e)
    {
      foreach (var connectorViewModel in this.ConnectorViewModels)
      {
        connectorViewModel.OnDoubleClick(sender, e);
      }
    }

    /// <summary>
    /// Sets the visiblity of column containing <see cref="ObservationState.Running" /> state indicator.
    /// </summary>
    private void SetRunningColumnVisible() => this.RunningConnectorColumnVisibility = this.RunningConnectors > 0 ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Sets the visiblity of column containing <see cref="ObservationState.Failure" /> state indicator.
    /// </summary>
    private void SetFailureColumnVisible() => this.FailureConnectorColumnVisibility = this.FailingConnectors > 0 ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Sets the visiblity of column containing <see cref="ObservationState.Unstable" /> state indicator.
    /// </summary>
    private void SetUnstableColumnVisible() => this.UnstableConnectorColumnVisibility = this.UnstableConnectors > 0 ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Sets the visiblity of column containing <see cref="ObservationState.Unknown" /> state indicator.
    /// </summary>
    private void SetUnknownColumnVisible() => this.UnknownConnectorColumnVisibility = this.UnknownConnectors > 0 ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Handles collection changed event of <see cref="ConnectorViewModel" />.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">Event args.</param>
    private void ConnectorViewModelsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
      this.UpdateConnectorWithGivenStatusCount();
      this.SetRunningColumnVisible();
      this.SetFailureColumnVisible();
      this.SetUnstableColumnVisible();
      this.SetUnknownColumnVisible();
    }

    private void UpdateConnectorWithGivenStatusCount()
    {
      this.FailingConnectors = this.ConnectorViewModels.Count(stat => stat.CurrentStatus.State == ObservationState.Failure);
      this.RunningConnectors = this.ConnectorViewModels.Count(stat => stat.CurrentStatus.State == ObservationState.Running);
      this.UnstableConnectors = this.ConnectorViewModels.Count(stat => stat.CurrentStatus.State == ObservationState.Unstable);
      this.UnknownConnectors = this.ConnectorViewModels.Count(stat => stat.CurrentStatus.State == ObservationState.Unknown);
    }

    private void CreateViewModelForConnectorConfiguration(ConnectorConfiguration connectorConfiguration)
    {
      var connector = PluginManager.Instance.GetConnector(connectorConfiguration);
      ConnectorViewModel connectorViewModel = connector == null
        ? new ConnectorMissingViewModel(connectorConfiguration)
        : this.GetConnectorViewModel(connector);
      connectorViewModel.EditItem += this.OnSubItemEdit;
      connectorViewModel.DeleteItem += this.DeleteConnector;
      connectorViewModel.ExportItem += this.ExportItemHandler;
      connectorViewModel.ConfigurationChanged += this.ConnectorViewModelConfigurationChanged;
      connectorViewModel.Update(connector);
      this.ConnectorViewModels.Add(connectorViewModel);
    }

    private void ConnectorViewModelConfigurationChanged(object? sender, EventArgs e) => this.OnConfigurationChanged(sender, e);

    /// <summary>
    /// Handles the export of a project item.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="ValueEventArgs{TreeItemViewModel}" /> instance containing the event data.</param>
    private void ExportItemHandler(object? sender, ValueEventArgs<TreeItemViewModel> e) => this.OnExportItem(this, e);

    private ConnectorViewModel GetConnectorViewModel(Connector connector)
    {
      var presentationPlugIn = PluginManager.Instance.GetPresentationPlugin(connector.Configuration.Type);
      if (presentationPlugIn != null)
      {
        return presentationPlugIn.CreateViewModel(connector);
      }

      return new ConnectorViewModel(connector);
    }

    private void OnSubItemEdit(object? sender, EditTreeItemViewModelEventArgs e) => this.OnEditItem(sender, e);

    private async void DeleteConnector(object? sender, DeleteTreeItemEventArgs e)
    {
      if (e.DeleteItem is ConnectorViewModel model)
      {
        this.OnDeleteItem(this, e);
      }
    }

    private void GroupNameChanged() => this.OnConfigurationChanged(this, EventArgs.Empty);
  }
}
