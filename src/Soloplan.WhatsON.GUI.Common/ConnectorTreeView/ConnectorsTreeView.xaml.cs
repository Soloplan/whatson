// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConnectorsTreeView.xaml.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.ConnectorTreeView
{
  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.Collections.ObjectModel;
  using System.ComponentModel;
  using System.Linq;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Data;
  using System.Windows.Input;
  using System.Windows.Markup;
  using System.Windows.Media;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common.BuildServer;
  using Soloplan.WhatsON.GUI.Common.VisualConfig;
  using Soloplan.WhatsON.Model;

  /// <summary>
  /// Interaction logic for ConnectorsTreeView.xaml.
  /// </summary>
  public partial class ConnectorsTreeView : UserControl, IDisposable
  {
    private ConnectorTreeViewModel model = new ConnectorTreeViewModel();

    private Collection<ConnectorViewModel> selectedConnectors = new Collection<ConnectorViewModel>();

    /// <summary>
    /// Backing field for <see cref="DeleteSelectedObject" />.
    /// </summary>
    private CustomCommand? deleteFocusedObject;

    /// <summary>
    /// Bool needed for double mouse up event firing problem. If there was an item clicked then, there will be no group
    /// clicked event handling succeeding the mentioned one.
    /// </summary>
    private bool connectorItemEventFired;

    public ConnectorsTreeView()
    {
      this.InitializeComponent();
      if (!DesignerProperties.GetIsInDesignMode(this))
      {
        foreach (var treeViewPresentationPlugIn in PluginManager.Instance.GetPresentationPlugins())
        {
          using (var resourceXml = treeViewPresentationPlugIn.GetDataTempletXaml())
          {
            if (resourceXml == null)
            {
              continue;
            }

            var dictionary = XamlReader.Load(resourceXml) as ResourceDictionary;
            this.Resources.MergedDictionaries.Add(dictionary);
          }
        }
      }

      this.KeyDown += this.OnKeyDown;
    }

    /// <summary>
    /// Event fired when configuration is changed by user interaction with <see cref="ConnectorsTreeView" />.
    /// </summary>
    public event EventHandler? ConfigurationChanged;

    /// <summary>
    /// Event fired when user requested editing of tree view item in context menu.
    /// </summary>
    public event EventHandler<EditTreeItemViewModelEventArgs>? EditItem;

    /// <summary>
    /// Event fired when user requested exporting of tree view item in context menu.
    /// </summary>
    public event EventHandler<ValueEventArgs<TreeItemViewModel>>? ExportItem;

    /// <summary>
    /// Event fired when user wants to delete item.
    /// </summary>
    public event EventHandler<DeleteTreeItemEventArgs>? DeleteItem;

    /// <summary>
    /// Gets command for deleting selected item.
    /// </summary>
    public CustomCommand DeleteSelectedObject => this.deleteFocusedObject ?? (this.deleteFocusedObject = this.CreateDeleteFocusedObjectCommand());

    public void Init(ObservationScheduler scheduler, ApplicationConfiguration configuration, IList<Connector> initialConnectorState)
    {
      this.Dispose();
      this.model = new ConnectorTreeViewModel();
      this.HookUnhookModelEvents(true);
      this.model.Init(scheduler, configuration, initialConnectorState);
      this.DataContext = this.model;
      this.SetupDataContext();
    }

    public void Update(ApplicationConfiguration configuration)
    {
      this.model.Update(configuration);
      this.SetupDataContext();
    }

    /// <summary>
    /// Writes current settings from <see cref="ConnectorsTreeView" /> to <paramref name="configuration" />.
    /// </summary>
    /// <param name="configuration">Configuration to which data should be written.</param>
    public void WriteToConfiguration(ApplicationConfiguration configuration) => this.model.WriteToConfiguration(configuration);

    public TreeListSettings GetTreeListSettings() => new TreeListSettings { GroupExpansions = this.model.GetGroupExpansionState() };

    public void ApplyTreeListSettings(TreeListSettings? treeListSettings)
    {
      if (treeListSettings == null)
      {
        return;
      }

      this.model.ApplyGroupExpansionState(treeListSettings.GroupExpansions);
    }

    /// <summary>
    /// Creates new group with <paramref name="groupName" /> and brings it into focus.
    /// </summary>
    /// <param name="groupName">Name for new group.</param>
    public void CreateGroup(string groupName)
    {
      var groupModel = this.model.CreateGroup(groupName);
      this.mainTreeView.UpdateLayout();
      if (this.mainTreeView.ItemContainerGenerator.ContainerFromItem(groupModel) is TreeViewItem groupViewItem)
      {
        groupViewItem.BringIntoView(new Rect(100, 100, 100, 100));
      }
    }

    /// <summary>
    /// Gets list of group names currently in use.
    /// </summary>
    /// <returns>List of currently used group names.</returns>
    public IList<string> GetGroupNames() => this.model.ConnectorGroups.Select(grp => grp.GroupName).ToList();

    /// <summary>
    /// Cleares some resources.
    /// </summary>
    public void Dispose()
    {
      this.HookUnhookModelEvents(false);
      this.model.Dispose();
      this.selectedConnectors.Clear();
      this.deleteFocusedObject = null;
      BindingOperations.ClearBinding(this.mainTreeView, TreeView.ItemsSourceProperty);
    }

    private void SetupDataContext()
    {
      IEnumerable? items = this.model.OneGroup && string.IsNullOrWhiteSpace(this.model.FirstGroup?.GroupName)
        ? this.model.FirstGroup?.ConnectorViewModels
        : this.model.ConnectorGroups;

      // Rebinding the same collection during its change event can duplicate generated rows.
      if (ReferenceEquals(this.mainTreeView.ItemsSource, items))
      {
        return;
      }

      BindingOperations.ClearBinding(this.mainTreeView, TreeView.ItemsSourceProperty);
      if (this.model.OneGroup && string.IsNullOrWhiteSpace(this.model.ConnectorGroups.FirstOrDefault()?.GroupName))
      {
        Binding myBinding = new Binding();
        myBinding.Source = this.model.FirstGroup;
        myBinding.Path = new PropertyPath("ConnectorViewModels");
        myBinding.Mode = BindingMode.OneWay;
        myBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        BindingOperations.SetBinding(this.mainTreeView, TreeView.ItemsSourceProperty, myBinding);
      }
      else
      {
        Binding myBinding = new Binding();
        myBinding.Source = this.model;
        myBinding.Path = new PropertyPath("ConnectorGroups");
        myBinding.Mode = BindingMode.OneWay;
        myBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        BindingOperations.SetBinding(this.mainTreeView, TreeView.ItemsSourceProperty, myBinding);
      }
    }

    private void OnTreeItemDoubleClick(object? sender, MouseButtonEventArgs e) => this.model.OnDoubleClick(sender, e);

    private CustomCommand CreateDeleteFocusedObjectCommand()
    {
      var command = new CustomCommand();
      command.OnExecute += (s, e) =>
      {
        if (this.selectedConnectors.Count == 0)
        {
          return;
        }

        var args = new DeleteTreeItemEventArgs(this.selectedConnectors[0]);
        this.model.DeleteGroupAsync(s, args);
      };

      return command;
    }

    private void ModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName == nameof(ConnectorTreeViewModel.OneGroup))
      {
        this.SetupDataContext();
      }
    }

    /// <summary>
    /// Hooks or unhooks events for <see cref="model" />.
    /// </summary>
    /// <param name="hook">Controls whether the events should be hooked or unhooked.</param>
    private void HookUnhookModelEvents(bool hook)
    {
      void OnModelOnEditItem(object? s, EditTreeItemViewModelEventArgs e)
      {
        this.EditItem?.Invoke(s, e);
      }

      void OnModelOnExportItem(object? s, ValueEventArgs<TreeItemViewModel> e)
      {
        this.ExportItem?.Invoke(s, e);
      }

      void OnModelOnDeleteItem(object? s, DeleteTreeItemEventArgs e)
      {
        this.DeleteItem?.Invoke(s, e);
      }

      void OnModelOnConfigurationChanged(object? s, EventArgs e)
      {
        this.ConfigurationChanged?.Invoke(this, EventArgs.Empty);
      }

      if (hook)
      {
        this.model.PropertyChanged += this.ModelPropertyChanged;
        this.model.EditItem += OnModelOnEditItem;
        this.model.ExportItem += OnModelOnExportItem;
        this.model.DeleteItem += OnModelOnDeleteItem;
        this.model.ConfigurationChanged += OnModelOnConfigurationChanged;
      }
      else
      {
        this.model.PropertyChanged -= this.ModelPropertyChanged;
        this.model.EditItem -= OnModelOnEditItem;
        this.model.ExportItem -= OnModelOnExportItem;
        this.model.DeleteItem -= OnModelOnDeleteItem;
        this.model.ConfigurationChanged -= OnModelOnConfigurationChanged;
      }
    }

    /// <summary>
    /// Restores the themed appearance of a tree view item.
    /// </summary>
    /// <param name="treeViewItem">Item to reset its style.</param>
    private void ResetStyle(ref TreeViewItem treeViewItem)
    {
      treeViewItem.ClearValue(BackgroundProperty);
      treeViewItem.ClearValue(BorderBrushProperty);
      treeViewItem.IsSelected = false;
    }

    /// <summary>
    /// Sets style for a tree view item.
    /// </summary>
    /// <param name="treeViewItem">Item to set its style.</param>
    private void SetStyle(ref TreeViewItem treeViewItem)
    {
      treeViewItem.SetResourceReference(BackgroundProperty, "SettingsSelection");
      treeViewItem.SetResourceReference(BorderBrushProperty, "SettingsAccent");
    }

    /// <summary>
    /// Finds a group where a connector is located and calls style reset for him.
    /// </summary>
    /// <param name="connector">Connector to reset its style.</param>
    private void ResetConnectorStyle(ConnectorViewModel connector)
    {
      foreach (var group in this.model.ConnectorGroups)
      {
        foreach (var itemInGroup in group.ConnectorViewModels)
        {
          if (itemInGroup.Identifier == connector.Identifier)
          {
            this.ResetConnectorInGroupStyle(group, itemInGroup);
          }
        }
      }
    }

    /// <summary>
    /// Finds a group where a connector is located and calls style set for him.
    /// </summary>
    /// <param name="connector">Connector to set its style.</param>
    private void SetConnectorStyle(ConnectorViewModel connector)
    {
      foreach (var group in this.model.ConnectorGroups)
      {
        foreach (var itemInGroup in group.ConnectorViewModels)
        {
          if (itemInGroup.Identifier == connector.Identifier)
          {
            this.SetConnectorInGroupStyle(group, itemInGroup);
          }
        }
      }
    }

    /// <summary>
    /// Based on group and connector function generates container from item and calls style reset for the generated
    /// container.
    /// </summary>
    /// <param name="connectorGroupViewModel">Group of the connector.</param>
    /// <param name="connectorViewModel">Connector to reset its style.</param>
    private void ResetConnectorInGroupStyle(ConnectorGroupViewModel connectorGroupViewModel, ConnectorViewModel connectorViewModel)
    {
      TreeViewItem groupTreeViewItem = (TreeViewItem)this.mainTreeView.ItemContainerGenerator.ContainerFromItem(connectorGroupViewModel);
      var treeViewItemInGroup = groupTreeViewItem?.ItemContainerGenerator.ContainerFromItem(connectorViewModel) as TreeViewItem
                                ?? (TreeViewItem)this.mainTreeView.ItemContainerGenerator.ContainerFromItem(connectorViewModel);
      if (treeViewItemInGroup != null)
      {
        this.ResetStyle(ref treeViewItemInGroup);
      }
    }

    /// <summary>
    /// Based on group and connector function generates container from item and calls style set for the generated container.
    /// </summary>
    /// <param name="connectorGroupViewModel">Group of the connector.</param>
    /// <param name="connectorViewModel">Connector to set its style.</param>
    private void SetConnectorInGroupStyle(ConnectorGroupViewModel connectorGroupViewModel, ConnectorViewModel connectorViewModel)
    {
      TreeViewItem groupTreeViewItem = (TreeViewItem)this.mainTreeView.ItemContainerGenerator.ContainerFromItem(connectorGroupViewModel);
      var treeViewItemInGroup = groupTreeViewItem?.ItemContainerGenerator.ContainerFromItem(connectorViewModel) as TreeViewItem
                                ?? (TreeViewItem)this.mainTreeView.ItemContainerGenerator.ContainerFromItem(connectorViewModel);
      if (treeViewItemInGroup != null)
      {
        this.SetStyle(ref treeViewItemInGroup);
      }
    }

    /// <summary>
    /// Restores order of a given list based on the order in the tree view.
    /// </summary>
    /// <param name="selectedList">Unordered list.</param>
    /// <returns>Ordered list of connectors.</returns>
    private Collection<ConnectorViewModel> GetListInTreeOrder(Collection<ConnectorViewModel> selectedList)
    {
      var sortedConnectors = new Collection<ConnectorViewModel>();
      foreach (var group in this.model.ConnectorGroups)
      {
        foreach (var itemInGroup in group.ConnectorViewModels)
        {
          foreach (var selectedConnector in this.selectedConnectors)
          {
            if (selectedConnector.Identifier == itemInGroup.Identifier)
            {
              sortedConnectors.Add(itemInGroup);
            }
          }
        }
      }

      return sortedConnectors;
    }

    /// <summary>
    /// Function determines behaviour after a connector in the tree was clicked with control pressed. It enables to toggle
    /// selection of a given connector.
    /// </summary>
    /// <param name="connector">Clicked connector.</param>
    private void OnCtrlProjectClicked(ConnectorViewModel connector)
    {
      if (this.IsConnectorSelected(connector))
      {
        this.DeselectConnector(connector);
      }
      else
      {
        this.SelectConnector(connector);
      }
    }

    /// <summary>
    /// Function determines behaviour after a connector in the tree was clicked with shift pressed. It allows to select items
    /// from last selected item to currently selected one.
    /// Allows selection between groups.
    /// </summary>
    /// <param name="connector">Clicked connector.</param>
    private void OnShiftProjectClicked(ConnectorViewModel connector)
    {
      if (this.selectedConnectors.Count == 0)
      {
        this.OnCtrlProjectClicked(connector);
        return;
      }

      ConnectorViewModel source = connector;
      ConnectorViewModel target = this.selectedConnectors.Last();
      Collection<ConnectorViewModel> ungroupedConnectors = new Collection<ConnectorViewModel>();
      foreach (var group in this.model.ConnectorGroups)
      {
        foreach (var connectorInGroup in group.ConnectorViewModels)
        {
          ungroupedConnectors.Add(connectorInGroup);
        }
      }

      if (source.Identifier == target.Identifier)
      {
        this.SelectConnector(connector);
        return;
      }

      bool select = false;
      foreach (var item in ungroupedConnectors)
      {
        if (item.Identifier == source.Identifier || item.Identifier == target.Identifier)
        {
          this.SelectConnector(item);
          select = !select;
        }
        else if (select)
        {
          this.SelectConnector(item);
        }
      }
    }

    /// <summary>
    /// Performs a selecection of a connector unless it has already been selected.
    /// </summary>
    /// <param name="connector">Connector to select.</param>
    private void SelectConnector(ConnectorViewModel connector)
    {
      if (this.IsConnectorSelected(connector))
      {
        return;
      }

      this.SetConnectorStyle(connector);
      this.selectedConnectors.Add(connector);
    }

    /// <summary>
    /// Performs a deselection of a connector.
    /// </summary>
    /// <param name="connector">Connector to deselect.</param>
    private void DeselectConnector(ConnectorViewModel connector)
    {
      if (!this.IsConnectorSelected(connector))
      {
        return;
      }

      this.ResetConnectorStyle(connector);
      this.selectedConnectors.Remove(connector);
    }

    /// <summary>
    /// Performs deselection for all connectors.
    /// </summary>
    private void DeselectAllConnectors()
    {
      foreach (var group in this.model.ConnectorGroups)
      {
        foreach (var connector in group.ConnectorViewModels)
        {
          this.DeselectConnector(connector);
        }
      }
    }

    /// <summary>
    /// Performs a selection for all connectors.
    /// </summary>
    private void SelectAllConnectors()
    {
      foreach (var group in this.model.ConnectorGroups)
      {
        foreach (var connector in group.ConnectorViewModels)
        {
          this.SelectConnector(connector);
        }
      }
    }

    /// <summary>
    /// Checks wether a connector is selected or not.
    /// </summary>
    /// <param name="connector">Connector to be checked.</param>
    /// <returns>True - if connector is selected, false - if connector is not selected.</returns>
    private bool IsConnectorSelected(ConnectorViewModel connector)
    {
      bool isAlreadyAdded = false;
      foreach (var selectedConnector in this.selectedConnectors)
      {
        if (selectedConnector.Identifier == connector.Identifier)
        {
          isAlreadyAdded = true;
        }
      }

      return isAlreadyAdded;
    }

    /// <summary>
    /// Checks if all connectors are selected.
    /// </summary>
    /// <returns>True when all connectors are selected or if there are no connectors at all, false otherwise.</returns>
    private bool AreAllconnectorsSelected()
    {
      bool allSelected = true;
      foreach (var group in this.model.ConnectorGroups)
      {
        foreach (var connector in group.ConnectorViewModels)
        {
          if (!this.IsConnectorSelected(connector))
          {
            allSelected = false;
          }
        }
      }

      return allSelected;
    }

    /// <summary>
    /// Defines behoaviour when a group is clicked. If not all connectors in the group are selected, then it selects them.
    /// If there are all connectors in the group selected, then the funcion deselects them.
    /// </summary>
    /// <param name="group">Group to check and de/select items in.</param>
    private void OnGroupClicked(ConnectorGroupViewModel group)
    {
      bool allConnectorsInGroupSelected = true;
      foreach (var connector in group.ConnectorViewModels)
      {
        if (!this.IsConnectorSelected(connector))
        {
          allConnectorsInGroupSelected = false;
        }
      }

      if (allConnectorsInGroupSelected)
      {
        foreach (var connector in group.ConnectorViewModels)
        {
          this.DeselectConnector(connector);
        }
      }
      else
      {
        foreach (var connector in group.ConnectorViewModels)
        {
          this.SelectConnector(connector);
        }
      }
    }

    /// <summary>
    /// Defines behaviour when an item in tree is clicked with Ctrl.
    /// </summary>
    /// <param name="sender">sender item.</param>
    /// <param name="e">event args.</param>
    private void ControlLeftMouseDownHandler(object? sender, MouseButtonEventArgs e)
    {
      var item = sender as TreeViewItem;
      if (item == null)
      {
        return;
      }

      var connector = new ConnectorViewModel();
      if (item.Header is ConnectorViewModel)
      {
        connector = (ConnectorViewModel)item.Header;
        this.OnCtrlProjectClicked(connector);
      }
      else
      {
        var group = new ConnectorGroupViewModel();
        if (item.Header is ConnectorGroupViewModel)
        {
          group = (ConnectorGroupViewModel)item.Header;
          this.OnGroupClicked(group);
        }
      }

      this.model.UpdateSelectedConnectors(this.selectedConnectors);
    }

    /// <summary>
    /// Defines behaviour when an item is clicked with shift pressed.
    /// </summary>
    /// <param name="sender">sender item.</param>
    /// <param name="e">event args.</param>
    private void ShiftLeftMouseDownHandler(object? sender, MouseButtonEventArgs e)
    {
      var item = sender as TreeViewItem;
      if (item == null)
      {
        return;
      }

      var connector = new ConnectorViewModel();
      if (item.Header is ConnectorViewModel)
      {
        connector = (ConnectorViewModel)item.Header;
        this.OnShiftProjectClicked(connector);
      }
    }

    /// <summary>
    /// Event handler for LMB down.
    /// </summary>
    private void OnTreeItemLeftMouseDown(object? sender, MouseButtonEventArgs e)
    {
      if (Keyboard.IsKeyDown(Key.LeftCtrl))
      {
        this.ControlLeftMouseDownHandler(sender, e);
      }
      else if (Keyboard.IsKeyDown(Key.LeftShift))
      {
        this.ShiftLeftMouseDownHandler(sender, e);
      }

      e.Handled = true;
    }

    /// <summary>
    /// Based on items selected count informs all items if there is only one item selected or not.
    /// </summary>
    private void ManageContextMenuAvailability()
    {
      if (this.selectedConnectors.Count > 1)
      {
        foreach (var group in this.model.ConnectorGroups)
        {
          foreach (var itemInGroup in group.ConnectorViewModels)
          {
            itemInGroup.IsOnlyOneSelected = false;
          }
        }
      }
      else
      {
        foreach (var group in this.model.ConnectorGroups)
        {
          foreach (var itemInGroup in group.ConnectorViewModels)
          {
            itemInGroup.IsOnlyOneSelected = true;
          }
        }
      }
    }

    /// <summary>
    /// Defines behaviour when LMB is released.
    /// </summary>
    /// <param name="sender">sender item.</param>
    /// <param name="e">event args.</param>
    private void ControlLeftMouseUpHandler(object? sender, MouseButtonEventArgs e)
    {
      var item = sender as TreeViewItem;

      var connector = new ConnectorViewModel();
      if (item == null)
      {
        return;
      }

      if (item.Header is ConnectorViewModel)
      {
        connector = (ConnectorViewModel)item.Header;
        this.DeselectAllConnectors();
        this.connectorItemEventFired = true;
        this.OnCtrlProjectClicked(connector);
      }
      else
      {
        if (this.connectorItemEventFired)
        {
          this.connectorItemEventFired = false;
        }
        else
        {
          var group = new ConnectorGroupViewModel();
          if (item.Header is ConnectorGroupViewModel)
          {
            group = (ConnectorGroupViewModel)item.Header;
            this.DeselectAllConnectors();
            this.OnGroupClicked(group);
          }
        }
      }

      this.model.UpdateSelectedConnectors(this.selectedConnectors);
    }

    /// <summary>
    /// Event handler for release of LMB.
    /// </summary>
    /// <param name="sender">sender item.</param>
    /// <param name="e">event args.</param>
    private void OnTreeItemLeftMouseUp(object? sender, MouseButtonEventArgs e)
    {
      if (Keyboard.IsKeyUp(Key.LeftCtrl) && Keyboard.IsKeyUp(Key.LeftShift))
      {
        this.ControlLeftMouseUpHandler(sender, e);
      }

      this.ManageContextMenuAvailability();
    }

    /// <summary>
    /// Performs some additional operations to inform model about selected connectors so the model can move/remove them when
    /// the time for a drop comes.
    /// </summary>
    /// <param name="sender">sender item.</param>
    /// <param name="e">event args.</param>
    private void OnTreeViewItemDrop(object? sender, DragEventArgs e)
    {
      if (sender is not TreeViewItem item)
      {
        return;
      }

      if (item.Header is ConnectorGroupViewModel)
      {
      }
      else
      {
        var connector = (ConnectorViewModel)item.Header;
        Collection<ConnectorViewModel> sortedConnectors = this.GetListInTreeOrder(this.selectedConnectors);
        this.model.UpdateConnectorsToDrop(sortedConnectors);
        this.DeselectAllConnectors();
        this.model.UpdateSelectedConnectors(this.selectedConnectors);
      }
    }

    /// <summary>
    /// Keeps context menus on the whole row, including gaps and padding.
    /// </summary>
    private void UpdateRowContextMenu(Border row)
    {
      row.ContextMenu = row.DataContext switch
      {
        ConnectorGroupViewModel => (ContextMenu)this.FindResource("BaseTreeMenuImpl"),
        ConnectorViewModel { CurrentStatus: BuildStatusViewModel } => (ContextMenu)this.FindResource("BuildServerProjectContextMenu"),
        _ => null,
      };
    }

    private void OnRowLoaded(object sender, RoutedEventArgs e) => this.UpdateRowContextMenu((Border)sender);

    private void OnRowDataContextChanged(object sender, DependencyPropertyChangedEventArgs e) => this.UpdateRowContextMenu((Border)sender);

    /// <summary>
    /// Defines behavoiour when RMB is used.
    /// </summary>
    /// <param name="sender">sender item.</param>
    /// <param name="e">event args.</param>
    private void OnTreeItemRightMouseDown(object? sender, MouseButtonEventArgs e)
    {
      if (sender is not TreeViewItem { Header: ConnectorViewModel connector } ||
          this.selectedConnectors.Any(selected => selected.Identifier == connector.Identifier))
      {
        return;
      }

      this.DeselectAllConnectors();
      this.model.UpdateSelectedConnectors(this.selectedConnectors);
      this.ManageContextMenuAvailability();
    }

    /// <summary>
    /// Event handler for key down event.
    /// </summary>
    /// <param name="sender">sender item.</param>
    /// <param name="e">event args.</param>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
      if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.A))
      {
        if (!this.AreAllconnectorsSelected())
        {
          this.SelectAllConnectors();
        }
        else
        {
          this.DeselectAllConnectors();
        }
      }
    }

    /// <summary>
    /// Handles scrolling event. Becuse of item virtualization it is necessary to refresh displayed item styles in code
    /// behind, because virtual items are null pointers when not seen.
    /// </summary>
    /// <param name="sender">Sender object.</param>
    /// <param name="e">Args.</param>
    private void OnScroll(object? sender, ScrollChangedEventArgs e)
    {
      if (this.model.ConnectorGroups == null)
      {
        return;
      }

      foreach (var group in this.model.ConnectorGroups)
      {
        foreach (var item in group.ConnectorViewModels)
        {
          if (this.IsConnectorSelected(item))
          {
            this.SetConnectorStyle(item);
          }
          else if (!this.IsConnectorSelected(item))
          {
            this.ResetConnectorStyle(item);
          }
        }
      }
    }
  }
}
