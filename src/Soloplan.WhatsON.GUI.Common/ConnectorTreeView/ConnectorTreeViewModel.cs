// <copyright file="ConnectorTreeViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.GUI.Common.ConnectorTreeView
{
  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.Collections.ObjectModel;
  using System.Linq;
  using System.Windows;
  using System.Windows.Input;
  using GongSolutions.Wpf.DragDrop;
  using NLog;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common.VisualConfig;
  using Soloplan.WhatsON.Model;

  /// <summary>
  /// Top level viewmodel used to bind to <see cref="ConnectorTreeView"/>.
  /// </summary>
  public class ConnectorTreeViewModel : NotifyPropertyChanged, IHandleDoubleClick, IDropTarget
  {
    /// <summary>
    /// The logger.
    /// </summary>
    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

    /// <summary>
    /// Backing field for <see cref="ConnectorGroups"/>.
    /// </summary>
    private ObservableCollection<ConnectorGroupViewModel> connectorGroups;

    /// <summary>
    /// Copy of currently selected connectors in case of multiple actions on viewmodel/>.
    /// </summary>
    private Collection<ConnectorViewModel> selectedConnectors;

    /// <summary>
    /// Flag indicating that <see cref="ConfigurationChanged"/> event is triggered - used to ignore updates of model.
    /// </summary>
    private bool configurationChanging;

    /// <summary>
    /// Backing field for <see cref="ItemPadding"/>.
    /// </summary>
    private int itemPadding;

    /// <summary>
    /// Previous value of <see cref="OneGroup"/>.
    /// </summary>
    private bool prevOneGroupValue;

    /// <summary>
    /// Previous value of relative instert position.
    /// </summary>
    private Collection<ConnectorViewModel> connectorsToDrop = new Collection<ConnectorViewModel>();

    private int fontSize;

    private int fontSizeSmall;

    private int buildIconSize;

    /// <summary>
    /// Called when configuration has changed due to user input.
    /// </summary>
    public event EventHandler ConfigurationChanged;

    /// <summary>
    /// Event fired when user requested editing of tree view item in context menu.
    /// </summary>
    public event EventHandler<EditTreeItemViewModelEventArgs> EditItem;

    /// <summary>
    /// Event fired when user requested exporting of tree view item in context menu.
    /// </summary>
    public event EventHandler<ValueEventArgs<TreeItemViewModel>> ExportItem;

    /// <summary>
    /// Event fired when item should be deleted.
    /// </summary>
    public event EventHandler<DeleteTreeItemEventArgs> DeleteItem;

    /// <summary>
    /// Gets observable collection of connector groups, the top level object in tree view binding.
    /// </summary>
    public ObservableCollection<ConnectorGroupViewModel> ConnectorGroups => this.connectorGroups ?? (this.connectorGroups = this.CreateConnectorGroupViewModelCollection());

    /// <summary>
    /// Gets first group from <see cref="ConnectorGroups"/>, used for binding when there is just one group <see cref="OneGroup"/>.
    /// </summary>
    public ConnectorGroupViewModel FirstGroup => this.ConnectorGroups.FirstOrDefault();

    /// <summary>
    /// Gets a value indicating whether there is only one group.
    /// </summary>
    public bool OneGroup => this.ConnectorGroups.Count == 1;

    /// <summary>
    /// Gets or sets the padding of tree view items.
    /// </summary>
    public int ItemPadding
    {
      get => this.itemPadding;
      set
      {
        if (this.itemPadding != value)
        {
          this.itemPadding = value;
          this.OnPropertyChanged();
        }
      }
    }

    public int FontSize
    {
      get => this.fontSize;
      set
      {
        if (this.fontSize != value)
        {
          this.fontSize = value;
          this.OnPropertyChanged();
        }
      }
    }

    public int FontSizeSmall
    {
      get => this.fontSizeSmall;
      set
      {
        if (this.fontSizeSmall != value)
        {
          this.fontSizeSmall = value;
          this.OnPropertyChanged();
        }
      }
    }

    public int BuildIconSize
    {
      get => this.buildIconSize;
      set
      {
        if (this.buildIconSize != value)
        {
          this.buildIconSize = value;
          this.OnPropertyChanged();
        }
      }
    }

    /// <summary>Updates the current drag state.</summary>
    /// <param name="dropInfo">Information about the drag.</param>
    /// <remarks>
    /// To allow a drop at the current drag position, the <see cref="P:GongSolutions.Wpf.DragDrop.DropInfo.Effects" /> property on
    /// <paramref name="dropInfo" /> should be set to a value other than <see cref="F:System.Windows.DragDropEffects.None" />
    /// and <see cref="P:GongSolutions.Wpf.DragDrop.DropInfo.Data" /> should be set to a non-null value.
    /// </remarks>
    public void DragOver(IDropInfo dropInfo)
    {
      if (object.ReferenceEquals(dropInfo.TargetItem, dropInfo.Data))
      {
        return;
      }

      if (dropInfo.Data is ConnectorViewModel)
      {
        if (dropInfo.TargetItem is ConnectorGroupViewModel)
        {
          dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
          dropInfo.Effects = DragDropEffects.Move;
        }
        else if (dropInfo.TargetItem is ConnectorViewModel)
        {
          dropInfo.Effects = DragDropEffects.Move;
          dropInfo.DropTargetAdorner = (dropInfo.InsertPosition & RelativeInsertPosition.TargetItemCenter) != 0 ? DropTargetAdorners.Highlight : DropTargetAdorners.Insert;
        }
      }
      else if (dropInfo.Data is ConnectorGroupViewModel)
      {
        if (dropInfo.TargetItem is ConnectorGroupViewModel)
        {
          dropInfo.Effects = DragDropEffects.Move;
          dropInfo.DropTargetAdorner = (dropInfo.InsertPosition & RelativeInsertPosition.TargetItemCenter) != 0 ? DropTargetAdorners.Highlight : DropTargetAdorners.Insert;
        }
      }
    }

    /// <summary>Performs a drop.</summary>
    /// <param name="dropInfo">Information about the drop.</param>
    public void Drop(IDropInfo dropInfo)
    {
      if (dropInfo.Effects != DragDropEffects.Move)
      {
        log.Warn("Unexpected drop operation. {data}", new { Effect = dropInfo.Effects, dropInfo.Data, Target = dropInfo.TargetItem });
        return;
      }

      var changesExist = false;
      if (dropInfo.Data is ConnectorGroupViewModel drggedGroup)
      {
        changesExist = this.DropGrup(dropInfo, drggedGroup);
      }
      else if (dropInfo.Data is ConnectorViewModel draggedConnector)
      {
        if (this.connectorsToDrop.Count != 0)
        {
          changesExist = this.DropConnector(dropInfo, draggedConnector);
          bool found = false;
          foreach (var connector in this.connectorsToDrop)
          {
            if (draggedConnector.Identifier == connector.Identifier)
            {
              found = true;
            }
          }

          if (found == true)
          {
            if (dropInfo.InsertPosition == GongSolutions.Wpf.DragDrop.RelativeInsertPosition.BeforeTargetItem)
            {
              var targetGroup = this.GetConnectorGroup((ConnectorViewModel)dropInfo.TargetItem);
              foreach (var element in this.connectorsToDrop.Reverse())
              {
                var sourceGroup = this.GetConnectorGroup(element);
                MoveObject(sourceGroup, targetGroup, dropInfo.InsertPosition);
                targetGroup = this.GetConnectorGroup(element);
              }
            }
            else
            {
              var targetGroup = this.GetConnectorGroup((ConnectorViewModel)dropInfo.TargetItem);
              foreach (var element in this.connectorsToDrop)
              {
                var sourceGroup = this.GetConnectorGroup(element);
                MoveObject(sourceGroup, targetGroup, dropInfo.InsertPosition);
                targetGroup = this.GetConnectorGroup(element);
              }
            }
          }

          this.connectorsToDrop = new Collection<ConnectorViewModel>();
        }
        else
        {
          changesExist = this.DropConnector(dropInfo, draggedConnector);
        }
      }

      if (changesExist)
      {
        this.OnConfigurationChanged(this, EventArgs.Empty);
      }
    }

    /// <summary>
    /// Initializes the model.
    /// </summary>
    /// <param name="scheduler">Scheduler used for observation.</param>
    /// <param name="configuration">Configuration.</param>
    /// <param name="initialConnectorState">List of currently observed connectors - provide start data for model.</param>
    public void Init(ObservationScheduler scheduler, ApplicationConfiguration configuration, IList<Connector> initialConnectorState)
    {
      log.Trace("Initializing {name}", nameof(ConnectorTreeViewModel));
      this.Update(configuration);
      foreach (var connector in initialConnectorState)
      {
        log.Trace("Applying status for connector {@connector}", connector);
        this.SchedulerStatusQueried(this, connector);
      }

      scheduler.StatusQueried -= this.SchedulerStatusQueried;
      scheduler.StatusQueried += this.SchedulerStatusQueried;
    }

    /// <summary>
    /// Updates the model.
    /// </summary>
    /// <param name="configuration">Changed configuration.</param>
    public void Update(ApplicationConfiguration configuration)
    {
      if (this.configurationChanging)
      {
        return;
      }

      log.Debug("Initializing {name}", nameof(ConnectorTreeViewModel));
      var grouping = this.ParseConfiguration(configuration).ToList();
      int index = 0;
      foreach (var group in grouping)
      {
        log.Debug("Applying settings for group {group}", group.Key);
        var connectorGroupViewModel = this.ConnectorGroups.FirstOrDefault(grp => grp.GroupName == group.Key);
        if (connectorGroupViewModel == null)
        {
          log.Debug("{model} doesn't exist, creating...", nameof(ConnectorGroupViewModel));
          connectorGroupViewModel = this.CreateNewGroupModel(group.Key);
          this.ConnectorGroups.Insert(index, connectorGroupViewModel);
        }
        else
        {
          var oldIndex = this.ConnectorGroups.IndexOf(connectorGroupViewModel);
          if (oldIndex != index)
          {
            this.ConnectorGroups.Move(oldIndex, index);
          }
        }

        index++;
        connectorGroupViewModel.Init(group);
      }

      var noLongerAvailable = this.ConnectorGroups.Where(grp => grouping.All(group => group.Key != grp.GroupName)).ToList();
      foreach (var connectorGroupViewModel in noLongerAvailable)
      {
        log.Debug("Removing group no longer present in configuration: {connectorGroupViewModel}", connectorGroupViewModel.GroupName);
        this.ConnectorGroups.Remove(connectorGroupViewModel);
      }

      foreach (var group in this.ConnectorGroups)
      {
        foreach (var projectViewModel in group.ConnectorViewModels)
        {
          projectViewModel.ApplyConfiguration(projectViewModel.Connector.Configuration);
        }
      }

      this.FireOneGroupChanged();
      switch (configuration.ViewStyle)
      {
        case ViewStyle.Normal:
          this.ItemPadding = 8;
          this.FontSize = 16;
          this.FontSizeSmall = 13;
          this.BuildIconSize = 18;
          break;
        case ViewStyle.Compact:
          this.ItemPadding = 4;
          this.FontSize = 14;
          this.FontSizeSmall = 12;
          this.BuildIconSize = 16;
          break;
        case ViewStyle.Pcked:
          this.ItemPadding = 0;
          this.FontSize = 12;
          this.FontSizeSmall = 10;
          this.BuildIconSize = 14;
          break;
      }

      this.SetConnectionModifiedInTree(false);
    }

    /// <summary>
    /// Writes model settings to <paramref name="configuration"/>.
    /// </summary>
    /// <param name="configuration">Application configuration.</param>
    public void WriteToConfiguration(ApplicationConfiguration configuration)
    {
      var connectorConfigurations = configuration.ConnectorsConfiguration.ToList();
      configuration.ConnectorsConfiguration.Clear();
      foreach (var connectorGroupViewModel in this.ConnectorGroups)
      {
        foreach (var connectorViewModel in connectorGroupViewModel.ConnectorViewModels)
        {
          var config = connectorConfigurations.FirstOrDefault(cfg => cfg.Identifier == connectorViewModel.Identifier);
          if (config != null)
          {
            config.GetConfigurationByKey(Connector.Category).Value = connectorGroupViewModel.GroupName;
            config.Name = connectorViewModel.Name;
          }

          configuration.ConnectorsConfiguration.Add(config);
        }
      }
    }

    public void OnDoubleClick(object sender, MouseButtonEventArgs e)
    {
      foreach (var connectorGroupViewModel in this.ConnectorGroups)
      {
        connectorGroupViewModel.OnDoubleClick(sender, e);
      }
    }

    /// <summary>
    /// Pares current expansion state of groups to <see cref="GroupExpansionSettings"/> list.
    /// </summary>
    /// <returns>List of <see cref="GroupExpansionSettings"/>.</returns>
    public IList<GroupExpansionSettings> GetGroupExpansionState()
    {
      return this.ConnectorGroups.Select(group => new GroupExpansionSettings
      {
        GroupName = group.GroupName,
        Expanded = group.IsNodeExpanded,
      }).ToList();
    }

    /// <summary>
    /// Applies <see cref="GroupExpansionSettings"/> for groups in this model.
    /// </summary>
    /// <param name="groupExpansion">Group expansion settings.</param>
    public void ApplyGroupExpansionState(IList<GroupExpansionSettings> groupExpansion)
    {
      if (groupExpansion == null)
      {
        return;
      }

      foreach (var expansion in groupExpansion)
      {
        var targetGroup = this.ConnectorGroups.FirstOrDefault(group => group.GroupName == expansion.GroupName);
        if (targetGroup != null)
        {
          targetGroup.IsNodeExpanded = expansion.Expanded;
        }

        log.Debug("Parsing expansion state for non-existing group {@GroupExpansion}", expansion);
      }
    }

    public ConnectorGroupViewModel CreateGroup(string groupName)
    {
      var model = this.CreateNewGroupModel(groupName);
      this.ConnectorGroups.Add(model);
      this.FireOneGroupChanged();
      this.OnConfigurationChanged(this, EventArgs.Empty);
      return model;
    }

    public bool UpdateConnectorsToDrop(Collection<ConnectorViewModel> list)
    {
      this.connectorsToDrop = list;
      return true;
    }

    public async void DeleteGroup(object sender, DeleteTreeItemEventArgs e)
    {
      if (e.DeleteItem is ConnectorViewModel clickedConnector)
      {
        if (this.selectedConnectors.Count > 1)
        {
          e.NoOtherSelections = false;
        }
        else
        {
          e.NoOtherSelections = true;
        }

        this.DeleteItem?.Invoke(sender, e);
        var canceled = await e.CheckCanceled();
        if (!canceled)
        {
          if (this.selectedConnectors.Count == 0)
          {
            this.DeleteConnector(clickedConnector);
            return;
          }

          this.DeleteSelectedConnectors();
        }

        return;
      }

      this.DeleteItem?.Invoke(sender, e);
      if (e.DeleteItem is ConnectorGroupViewModel group)
      {
        var canceled = await e.CheckCanceled();
        if (!canceled && this.ConnectorGroups.Remove(group))
        {
          this.OnConfigurationChanged(this, EventArgs.Empty);
          this.FireOneGroupChanged();
        }
      }
    }

    /// <summary>
    /// Updates the currently selected items form view.
    /// </summary>
    public void UpdateSelectedConnectors(Collection<ConnectorViewModel> selectedConnectors)
    {
      this.selectedConnectors = selectedConnectors;
    }

    /// <summary>
    /// Calls <see cref="ConfigurationChanged"/> event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="args">Event args.</param>
    protected void OnConfigurationChanged(object sender, EventArgs args)
    {
      try
      {
        this.configurationChanging = true;
        this.ConfigurationChanged?.Invoke(sender, args);
        this.SetConnectionModifiedInTree(true);
      }
      finally
      {
        this.configurationChanging = false;
      }
    }

    /// <summary>
    /// Moves object given by <paramref name="source"/> to <paramref name="target"/>.
    /// </summary>
    /// <param name="source">Information about source location of moved object.</param>
    /// <param name="target">Information about desired location of moved object.</param>
    /// <param name="insertPosition">Additional information about where in relation to <paramref name="target"/> the object should be placed.</param>
    /// <returns>True if operation caused changes; false otherwise.</returns>
    private static bool MoveObject(MovedObjectLocation source, MovedObjectLocation target, RelativeInsertPosition insertPosition)
    {
      var insertPositionInternal = insertPosition;
      if ((insertPositionInternal & RelativeInsertPosition.TargetItemCenter) != 0)
      {
        insertPositionInternal = RelativeInsertPosition.AfterTargetItem;
      }

      var targetIndex = target.Index;
      if ((insertPositionInternal & RelativeInsertPosition.AfterTargetItem) != 0)
      {
        targetIndex = targetIndex + 1;
      }

      if (object.ReferenceEquals(target.List, source.List))
      {
        if (targetIndex > source.Index)
        {
          targetIndex = targetIndex - 1;
        }

        if (source.Index == targetIndex)
        {
          return false;
        }
      }

      var obj = source.List[source.Index];
      source.List.RemoveAt(source.Index);
      target.List.Insert(targetIndex, obj);

      return true;
    }

    private IEnumerable<IGrouping<string, ConnectorConfiguration>> ParseConfiguration(ApplicationConfiguration configuration)
    {
      return configuration.ConnectorsConfiguration.GroupBy(config => config.GetConfigurationByKey(Connector.Category)?.Value?.Trim() ?? string.Empty);
    }

    /// <summary>
    /// Handles dropping of <see cref="ConnectorViewModel"/>.
    /// </summary>
    /// <param name="dropInfo">All drop information.</param>
    /// <param name="droppedConnector">The dropped connector.</param>
    /// <returns>True if operation caused changes; false otherwise.</returns>
    private bool DropConnector(IDropInfo dropInfo, ConnectorViewModel droppedConnector)
    {
      var currentConnectorGroupModel = this.GetConnectorGroup(droppedConnector);
      if (dropInfo.TargetItem is ConnectorGroupViewModel model)
      {
        if (object.ReferenceEquals(currentConnectorGroupModel.List, model.ConnectorViewModels))
        {
          return false;
        }

        return MoveObject(currentConnectorGroupModel, new MovedObjectLocation(model.ConnectorViewModels, model.ConnectorViewModels.Count - 1), RelativeInsertPosition.AfterTargetItem);
      }

      if (dropInfo.TargetItem is ConnectorViewModel targetModel)
      {
        var targetGroup = this.GetConnectorGroup(targetModel);
        return MoveObject(currentConnectorGroupModel, targetGroup, dropInfo.InsertPosition);
      }

      return false;
    }

    /// <summary>
    /// Handles dropping of <see cref="ConnectorGroupViewModel"/>.
    /// </summary>
    /// <param name="dropInfo">All drop information.</param>.
    /// <param name="droppedGroup">The dropped group.</param>
    /// <returns>True if operation caused changes; false otherwise.</returns>
    private bool DropGrup(IDropInfo dropInfo, ConnectorGroupViewModel droppedGroup)
    {
      if (dropInfo.TargetItem is ConnectorGroupViewModel targetModel)
      {
        var index = this.ConnectorGroups.IndexOf(droppedGroup);
        var targetIndex = this.ConnectorGroups.IndexOf(targetModel);
        return MoveObject(new MovedObjectLocation(this.ConnectorGroups, index), new MovedObjectLocation(this.ConnectorGroups, targetIndex), dropInfo.InsertPosition);
      }

      return false;
    }

    /// <summary>
    /// Gets information about location in parent collection.
    /// </summary>
    /// <param name="connectorViewModel">The connector view model.</param>
    /// <returns>The information about location in target collection.</returns>
    private MovedObjectLocation GetConnectorGroup(ConnectorViewModel connectorViewModel)
    {
      foreach (var connectorGroupViewModel in this.ConnectorGroups)
      {
        var index = connectorGroupViewModel.ConnectorViewModels.IndexOf(connectorViewModel);
        if (index < 0)
        {
          continue;
        }

        return new MovedObjectLocation(connectorGroupViewModel.ConnectorViewModels, index);
      }

      return null;
    }

    private void SchedulerStatusQueried(object sender, Connector e)
    {
      foreach (var connectorGroupViewModel in this.ConnectorGroups)
      {
        if (connectorGroupViewModel.Update(e))
        {
          return;
        }
      }

      log.Warn("No viewmodel found for connector {@Connector}", e);
    }

    private ObservableCollection<ConnectorGroupViewModel> CreateConnectorGroupViewModelCollection()
    {
      return new ObservableCollection<ConnectorGroupViewModel>();
    }

    private ConnectorGroupViewModel CreateNewGroupModel(string groupName)
    {
      var model = new ConnectorGroupViewModel
      {
        GroupName = groupName,
      };

      model.EditItem += (s, e) => this.EditItem?.Invoke(s, e);
      model.DeleteItem += this.DeleteGroup;
      model.ExportItem += this.ExportHandler;
      model.ConfigurationChanged += (s, e) => this.OnConfigurationChanged(this, EventArgs.Empty);
      return model;
    }

    /// <summary>
    /// Handles the export of a project.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="ValueEventArgs{TreeItemViewModel}"/> instance containing the event data.</param>
    private void ExportHandler(object sender, ValueEventArgs<TreeItemViewModel> e)
    {
      this.ExportItem?.Invoke(sender, e);
    }

    /// <summary>
    /// Removes any empty rpesent groups.
    /// </summary>
    private void ClearEmptyGroups()
    {
      foreach (var group in this.connectorGroups.ToList())
      {
        if (group.ConnectorViewModels.Count == 0)
        {
          this.connectorGroups.Remove(group);
        }
      }
    }

    /// <summary>
    /// Performs a deletion of all selected connectors.
    /// </summary>
    private void DeleteSelectedConnectors()
    {
      bool madeChanges = false;
      foreach (var connectorGroup in this.connectorGroups.ToList())
      {
        foreach (var connector in connectorGroup.ConnectorViewModels.ToList())
        {
          for (int i = 0; i < this.selectedConnectors.Count;)
          {
            if (this.selectedConnectors[i].Identifier == connector.Identifier)
            {
              connectorGroup.ConnectorViewModels.Remove(this.selectedConnectors[i]);
              this.selectedConnectors.RemoveAt(i);
              madeChanges = true;
              i--;
            }

            i++;
          }
        }
      }

      this.ClearEmptyGroups();

      if (madeChanges)
      {
        this.OnConfigurationChanged(this, EventArgs.Empty);
        this.FireOneGroupChanged();
      }
    }

    /// <summary>
    /// Performs a deletion of a connector.
    /// </summary>
    private void DeleteConnector(ConnectorViewModel connectorViewModel)
    {
      bool madeChanges = false;
      foreach (var connectorGroup in this.connectorGroups.ToList())
      {
        foreach (var connector in connectorGroup.ConnectorViewModels.ToList())
        {
          if (connectorViewModel.Identifier == connector.Identifier)
          {
            connectorGroup.ConnectorViewModels.Remove(connectorViewModel);
            madeChanges = true;
          }
        }
      }

      this.ClearEmptyGroups();

      if (madeChanges)
      {
        this.OnConfigurationChanged(this, EventArgs.Empty);
        this.FireOneGroupChanged();
      }
    }

    private void FireOneGroupChanged()
    {
      if (this.OneGroup != this.prevOneGroupValue)
      {
        this.OnPropertyChanged(nameof(this.OneGroup));
        this.prevOneGroupValue = this.OneGroup;
      }
    }

    /// <summary>
    /// Sets <see cref="TreeItemViewModel.ConfigurationModifiedInTree"/> of groups.
    /// </summary>
    /// <param name="value">The value which should be set.</param>
    private void SetConnectionModifiedInTree(bool value)
    {
      foreach (var connectorGroupViewModel in this.ConnectorGroups)
      {
        connectorGroupViewModel.ConfigurationModifiedInTree = value;
      }
    }

    /// <summary>
    /// Helper class with information about where the moved object is or should be in <see cref="List"/>.
    /// </summary>
    public class MovedObjectLocation
    {
      public MovedObjectLocation(IList list, int index)
      {
        this.List = list;
        this.Index = index;
      }

      /// <summary>
      /// Gets the source/target list.
      /// </summary>
      public IList List { get; }

      /// <summary>
      /// Gets the actual/desired location of object.
      /// </summary>
      public int Index { get; }
    }
  }
}