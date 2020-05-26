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
  using System.Globalization;
  using System.Linq;
  using System.ServiceModel.Security.Tokens;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Data;
  using System.Windows.Input;
  using System.Windows.Markup;
  using System.Windows.Media;
  using GongSolutions.Wpf.DragDrop;
  using Humanizer.Localisation;
  using MaterialDesignThemes.Wpf;
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

    private Collection<ConnectorViewModel> selectedConnectors;

    /// <summary>
    /// Backing field for <see cref="DeleteSelectedObject"/>.
    /// </summary>
    private CustomCommand deleteFocusedObject;

    public ConnectorsTreeView()
    {
      selectedConnectors = new Collection<ConnectorViewModel>();
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

    private static Color HexToColor(string hexColor)
    {
      if (hexColor.IndexOf('#') != -1)
      {
        hexColor = hexColor.Replace("#", "");
      }

      byte red = 0;
      byte green = 0;
      byte blue = 0;

      if (hexColor.Length == 8)
      {
        hexColor = hexColor.Substring(2);
      }

      if (hexColor.Length == 6)
      {
        red = byte.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
        green = byte.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
        blue = byte.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
      }
      else if (hexColor.Length == 3)
      {
        red = byte.Parse(hexColor[0].ToString() + hexColor[0].ToString(), NumberStyles.AllowHexSpecifier);
        green = byte.Parse(hexColor[1].ToString() + hexColor[1].ToString(), NumberStyles.AllowHexSpecifier);
        blue = byte.Parse(hexColor[2].ToString() + hexColor[2].ToString(), NumberStyles.AllowHexSpecifier);
      }

      return Color.FromRgb(red, green, blue);
    }

    private Brush InvertColor(string value)
    {
      if (value != null)
      {
        Color colorToConvert = HexToColor(value);
        Color invertedColor = Color.FromRgb((byte)~colorToConvert.R, (byte)~colorToConvert.G, (byte)~colorToConvert.B);
        return new SolidColorBrush(invertedColor);
      }
      else
      {
          return new SolidColorBrush(Color.FromRgb(0,0,0));
      }
    }

    private void ResetStyle(ref TreeViewItem treeViewItem)
    {
      var style = this.FindResource("MaterialDesignBackground");
      treeViewItem.Foreground = this.InvertColor(style.ToString());
    }

    private void SetStyle(ref TreeViewItem treeViewItem)
    {
      treeViewItem.Foreground = Brushes.MediumVioletRed;
    }

    private void ProjectClicked(ConnectorViewModel connector)
    {
      foreach (var groupViewModel in this.model.ConnectorGroups)
      {
        foreach (var connectorViewModel in groupViewModel.ConnectorViewModels)
        {
          if (connectorViewModel.Connector.Configuration.Identifier == connector.Identifier)
          {
            TreeViewItem groupViewItem = (TreeViewItem)this.mainTreeView.ItemContainerGenerator.ContainerFromItem(groupViewModel);
            var treeViewItem = (TreeViewItem)groupViewItem?.ItemContainerGenerator.ContainerFromItem(connectorViewModel)
              ?? (TreeViewItem)this.mainTreeView.ItemContainerGenerator.ContainerFromItem(connectorViewModel);
            bool isAlreadyAdded = false;

            foreach (var selectedConnector in selectedConnectors)
            {
              if (selectedConnector.Identifier == connector.Identifier)
              {
                isAlreadyAdded = true;
                foreach (var group in this.model.ConnectorGroups)
                {
                  foreach (var itemInGroup in group.ConnectorViewModels)
                  {
                    if (itemInGroup.Connector.Configuration.Identifier == selectedConnector.Identifier)
                    {
                      TreeViewItem groupTreeViewItem = (TreeViewItem)this.mainTreeView.ItemContainerGenerator.ContainerFromItem(group);
                      var treeViewItemInGroup = (TreeViewItem)groupTreeViewItem?.ItemContainerGenerator.ContainerFromItem(itemInGroup)
                        ?? (TreeViewItem)this.mainTreeView.ItemContainerGenerator.ContainerFromItem(itemInGroup);
                      this.ResetStyle(ref treeViewItemInGroup);
                      this.selectedConnectors.Remove(selectedConnector);
                    }
                  }
                }
              }
            }

            if (isAlreadyAdded == false)
            {
              this.SetStyle(ref treeViewItem);
              this.selectedConnectors.Add(connector);
            }
          }
        }
      }
    }

    private void OnTreeItemLeftMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (Keyboard.IsKeyDown(Key.LeftShift))
      {
        TreeViewItem item = (TreeViewItem)sender;
        if (item == null)
        {
          return;
        }

        var connector = new ConnectorViewModel();
        try
        {
          connector = (ConnectorViewModel)item.Header;
          this.ProjectClicked(connector);
        }
        catch
        {
          var group = new ConnectorGroupViewModel();
          try
          {
            group = (ConnectorGroupViewModel)item.Header;
          }
          catch (Exception ex)
          {
            //todo
          }
        }
      }

      e.Handled = true;
      return;
    }

    private void OnTreeItemLeftMouseUp(object sender, MouseButtonEventArgs e)
    {
      if (Keyboard.IsKeyUp(Key.LeftShift))
      {
        if (selectedConnectors.Count > 0)
        {
          foreach (var group in this.model.ConnectorGroups)
          {
            foreach (var itemInGroup in group.ConnectorViewModels)
            {
              TreeViewItem groupTreeViewItem = (TreeViewItem)this.mainTreeView.ItemContainerGenerator.ContainerFromItem(group);
              var treeViewItemInGroup = (TreeViewItem)groupTreeViewItem?.ItemContainerGenerator.ContainerFromItem(itemInGroup)
                ?? (TreeViewItem)this.mainTreeView.ItemContainerGenerator.ContainerFromItem(itemInGroup);
              this.ResetStyle(ref treeViewItemInGroup);
            }
          }

          this.selectedConnectors.Clear();
        }

        TreeViewItem item = (TreeViewItem)sender;
        try
        {
          var connector = new ConnectorViewModel();
          if (item == null)
          {
            return;
          }
          connector = (ConnectorViewModel)item.Header;
          this.ProjectClicked(connector);
        }
        catch
        {
          var group = new ConnectorGroupViewModel();
          try
          {
            group = (ConnectorGroupViewModel)item.Header;
          }
          catch (Exception ex)
          {
            //todo
          }
        }
      }
    }

    private void TreeViewItem_Drop(object sender, DragEventArgs e)
    {
      var item = (TreeViewItem)sender;
      if (item.Header is ConnectorGroupViewModel)
      {
        return;
      }

      var connector = (ConnectorViewModel)item.Header;
      Collection<ConnectorViewModel> sortedConnectors = new Collection<ConnectorViewModel>();
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

      model.MoveListAfter(sortedConnectors, connector, sender, e);

      foreach (var group in this.model.ConnectorGroups)
      {
        foreach (var itemInGroup in group.ConnectorViewModels)
        {
          TreeViewItem groupTreeViewItem = (TreeViewItem)this.mainTreeView.ItemContainerGenerator.ContainerFromItem(group);
          var treeViewItemInGroup = (TreeViewItem)groupTreeViewItem?.ItemContainerGenerator.ContainerFromItem(itemInGroup)
            ?? (TreeViewItem)this.mainTreeView.ItemContainerGenerator.ContainerFromItem(itemInGroup);
          this.ResetStyle(ref treeViewItemInGroup);
        }
      }

      this.selectedConnectors.Clear();
    }
  }
}
