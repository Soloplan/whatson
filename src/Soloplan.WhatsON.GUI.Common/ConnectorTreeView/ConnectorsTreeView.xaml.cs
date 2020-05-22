// <copyright file="ConnectorsTreeView.xaml.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.GUI.Common.ConnectorTreeView
{
  using System;
  using System.Collections.Generic;
  using System.Collections.ObjectModel;
  using System.ComponentModel;
  using System.Linq;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Data;
  using System.Windows.Input;
  using System.Windows.Markup;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common.VisualConfig;
  using Soloplan.WhatsON.Model;

  /// <summary>
  /// Interaction logic for ConnectorsTreeView.xaml.
  /// </summary>
  public partial class ConnectorsTreeView : UserControl, IDisposable
  {
    private ConnectorTreeViewModel model;

    private Collection<Connector> selectedConnectors;

    /// <summary>
    /// Backing field for <see cref="DeleteSelectedObject"/>.
    /// </summary>
    private CustomCommand deleteFocusedObject;

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
    }

    /// <summary>
    /// Event fired when configuration is changed by user interaction with <see cref="ConnectorsTreeView"/>.
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
    /// Event fired when user wants to delete item.
    /// </summary>
    public event EventHandler<DeleteTreeItemEventArgs> DeleteItem;

    /// <summary>
    /// Gets command for deleting selected item.
    /// </summary>
    public CustomCommand DeleteSelectedObject => this.deleteFocusedObject ?? (this.deleteFocusedObject = this.CreateDeleteFocusedObjectCommand());

    public void Init(ObservationScheduler scheduler, ApplicationConfiguration configuration, IList<Connector> initialConnectorState)
    {
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
    /// Writes current settings from <see cref="ConnectorsTreeView"/> to <paramref name="configuration"/>.
    /// </summary>
    /// <param name="configuration">Configuration to which data should be written.</param>
    public void WriteToConfiguration(ApplicationConfiguration configuration)
    {
      this.model.WriteToConfiguration(configuration);
    }

    public TreeListSettings GetTreeListSettings()
    {
      return new TreeListSettings { GroupExpansions = this.model.GetGroupExpansionState() };
    }

    public void ApplyTreeListSettings(TreeListSettings treeListSettings)
    {
      if (treeListSettings == null)
      {
        return;
      }

      this.model.ApplyGroupExpansionState(treeListSettings.GroupExpansions);
    }

    /// <summary>
    /// Focuses the node connected with <paramref name="connector" />.
    /// </summary>
    /// <param name="connector">Connector which should be focused.</param>
    public void FocusItem(Connector connector)
    {
      foreach (var groupViewModel in this.model.ConnectorGroups)
      {
        foreach (var connectorViewModel in groupViewModel.ConnectorViewModels)
        {
          if (connectorViewModel.Connector.Configuration.Identifier == connector.Configuration.Identifier)
          {
            TreeViewItem groupViewItem = (TreeViewItem)this.mainTreeView.ItemContainerGenerator.ContainerFromItem(groupViewModel);
            var treeViewItem = (TreeViewItem)groupViewItem?.ItemContainerGenerator.ContainerFromItem(connectorViewModel)
              ?? (TreeViewItem)this.mainTreeView.ItemContainerGenerator.ContainerFromItem(connectorViewModel);
            if (treeViewItem != null)
            {
              groupViewModel.IsNodeExpanded = true;
              treeViewItem.IsSelected = true;
              treeViewItem.BringIntoView(new Rect(100, 100, 100, 100));
            }
          }
        }
      }
    }

    /// <summary>
    /// Creates new group with <paramref name="groupName"/> and brings it into focus.
    /// </summary>
    /// <param name="groupName">Name for new group.</param>
    public void CreateGroup(string groupName)
    {
      var groupModel = this.model.CreateGroup(groupName);
      TreeViewItem groupViewItem = (TreeViewItem)this.mainTreeView.ItemContainerGenerator.ContainerFromItem(groupModel);
      groupViewItem.BringIntoView(new Rect(100, 100, 100, 100));
    }

    /// <summary>
    /// Gets list of group names currently in use.
    /// </summary>
    /// <returns>List of currently used group names.</returns>
    public IList<string> GetGroupNames()
    {
      return this.model.ConnectorGroups.Select(grp => grp.GroupName).ToList();
    }

    /// <summary>
    /// Cleares some resources.
    /// </summary>
    public void Dispose()
    {
      this.HookUnhookModelEvents(false);
      this.model = null;
      this.deleteFocusedObject = null;
      BindingOperations.ClearBinding(this.mainTreeView, TreeView.ItemsSourceProperty);
    }

    private void SetupDataContext()
    {
      BindingOperations.ClearBinding(this.mainTreeView, TreeView.ItemsSourceProperty);
      if (this.model.OneGroup && string.IsNullOrWhiteSpace(this.model.ConnectorGroups.FirstOrDefault().GroupName))
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

    private void OnTreeItemDoubleClick(object sender, MouseButtonEventArgs e)
    {
      this.model.OnDoubleClick(sender, e);
    }

    private CustomCommand CreateDeleteFocusedObjectCommand()
    {
      var command = new CustomCommand();
      command.OnExecute += (s, e) =>
      {
        if (this.mainTreeView.SelectedItem is TreeItemViewModel model)
        {
          model.DeleteCommand.Execute(null);
        }
      };

      return command;
    }

    private void ModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName == nameof(ConnectorTreeViewModel.OneGroup))
      {
        this.SetupDataContext();
      }
    }

    /// <summary>
    /// Hooks or unhooks events for <see cref="model"/>.
    /// </summary>
    /// <param name="hook">Controls whether the events should be hooked or unhooked.</param>
    private void HookUnhookModelEvents(bool hook)
    {
      void OnModelOnEditItem(object s, EditTreeItemViewModelEventArgs e) => this.EditItem?.Invoke(s, e);
      void OnModelOnExportItem(object s, ValueEventArgs<TreeItemViewModel> e) => this.ExportItem?.Invoke(s, e);
      void OnModelOnDeleteItem(object s, DeleteTreeItemEventArgs e) => this.DeleteItem?.Invoke(s, e);
      void OnModelOnConfigurationChanged(object s, EventArgs e) => this.ConfigurationChanged?.Invoke(this, EventArgs.Empty);

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

    private void OnTreeViewItemMouseDown(object sender, MouseButtonEventArgs e)
    {
      return;
    }



    private void OnTreeItemLeftMouseDown(object sender, MouseButtonEventArgs e)
    {
      TreeViewItem item = (TreeViewItem)sender;
      if (item == null)
      {
        return;
      }
      else
      {
        if (Keyboard.IsKeyDown(Key.LeftShift))
        {
          //item.Focus();
          item.IsSelected = true;
        }
        else
        {
          foreach (var groupViewModel in mainTreeView.Items)
          {
            //TreeViewItem groupViewItem = (TreeViewItem)this.mainTreeView.ItemContainerGenerator.ContainerFromItem(groupViewModel);
            //var treeViewItem = (TreeViewItem)groupViewItem?.ItemContainerGenerator.ContainerFromItem(connectorViewModel);
          }
          //item.Focus();
          item.IsSelected = true;
          var connector = (Connector)item.Header;
          selectedConnectors.Add(connector);
        }
      }
      e.Handled = true;
      return;
    }

    private void TreeViewItem_Drop(object sender, DragEventArgs e)
    {
      int selectedItemCounter = 0;
      foreach (var groupViewModel in this.model.ConnectorGroups)
      {
        foreach (var connectorViewModel in groupViewModel.ConnectorViewModels)
        {
          TreeViewItem groupViewItem = (TreeViewItem)this.mainTreeView.ItemContainerGenerator.ContainerFromItem(groupViewModel);
          var treeViewItem = (TreeViewItem)groupViewItem?.ItemContainerGenerator.ContainerFromItem(connectorViewModel)
            ?? (TreeViewItem)this.mainTreeView.ItemContainerGenerator.ContainerFromItem(connectorViewModel);
          if (treeViewItem != null)
          {
            if (treeViewItem.IsSelected == true)
            {
              selectedItemCounter++;
            }
          }
        }
        selectedItemCounter = selectedItemCounter;
        return;
      }
    }
  }
}
