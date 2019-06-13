namespace Soloplan.WhatsON.GUI.Common.ConnectorTreeView
{
  using System;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Linq;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Data;
  using System.Windows.Input;
  using System.Windows.Markup;
  using Soloplan.WhatsON.GUI.Common.VisualConfig;
  using Soloplan.WhatsON.Serialization;

  /// <summary>
  /// Interaction logic for ConnectorsTreeView.xaml
  /// </summary>
  public partial class ConnectorsTreeView : UserControl
  {
    private ConnectorTreeViewModel model;

    public ConnectorsTreeView()
    {
      this.InitializeComponent();
      if (!DesignerProperties.GetIsInDesignMode(this))
      {
        foreach (var treeViewPresentationPlugIn in PluginsManager.Instance.GetPresentationPlugIns())
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
    public event EventHandler<ValueEventArgs<TreeItemViewModel>> EditItem;

    public void Init(ObservationScheduler scheduler, ApplicationConfiguration configuration, IList<Connector> initialConnectorState)
    {
      this.model = new ConnectorTreeViewModel();
      this.model.EditItem += (s, e) => this.EditItem?.Invoke(s, e);
      this.model.ConfigurationChanged += (s, e) => this.ConfigurationChanged?.Invoke(this, EventArgs.Empty);
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
      return new TreeListSettings
      {
        GroupExpansions = this.model.GetGroupExpansionState()
      };
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
    /// Focuses the node connected with <paramref name="connector>.
    /// </summary>
    /// <param name="connector">Connector which should be focused.</param>
    public void FocusItem(Connector connector)
    {
      foreach (var groupViewModel in this.model.ConnectorGroups)
      {
        foreach (var connectorViewModel in groupViewModel.ConnectorViewModels)
        {
          if (connectorViewModel.Connector.ConnectorConfiguration.Identifier == connector.ConnectorConfiguration.Identifier)
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
      var onlyOneGroupBkp = this.model.OneGroup;
      var groupModel = this.model.CreateGroup(groupName);
      TreeViewItem groupViewItem = (TreeViewItem)this.mainTreeView.ItemContainerGenerator.ContainerFromItem(groupModel);
      groupViewItem.BringIntoView(new Rect(100, 100, 100, 100));
      if (onlyOneGroupBkp)
      {
        this.SetupDataContext();
      }
    }

    /// <summary>
    /// Gets list of group names currently in use.
    /// </summary>
    /// <returns>List of currently used group names.</returns>
    public IList<string> GetGroupNames()
    {
      return this.model.ConnectorGroups.Select(grp => grp.GroupName).ToList();
    }

    private void SetupDataContext()
    {
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
  }
}
