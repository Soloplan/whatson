namespace Soloplan.WhatsON.GUI.SubjectTreeView
{
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Data;
  using System.Windows.Input;
  using System.Windows.Markup;
  using Soloplan.WhatsON.GUI.VisualConfig;
  using Soloplan.WhatsON.Serialization;

  /// <summary>
  /// Interaction logic for SubjectsTreeView.xaml
  /// </summary>
  public partial class SubjectsTreeView : UserControl
  {
    public SubjectTreeViewModel model;

    public SubjectsTreeView()
    {
      this.InitializeComponent();
      if (!DesignerProperties.GetIsInDesignMode(this))
      {
        foreach (var treeViewPresentationPlugIn in PluginsManager.Instance.GetPresentationPlugIns())
        {
          using (var resourceXml = treeViewPresentationPlugIn.GetDataTempletXaml())
          {
            var dictionary = XamlReader.Load(resourceXml) as ResourceDictionary;
            this.Resources.MergedDictionaries.Add(dictionary);
          }
        }
      }
    }

    public void Init(ObservationScheduler scheduler, ApplicationConfiguration configuration, IList<Subject> initialSubjectState)
    {
      this.model = new SubjectTreeViewModel();
      this.model.Init(scheduler, configuration, initialSubjectState);
      this.DataContext = this.model;
      this.SetupDataContext();
    }

    public void Update(ApplicationConfiguration configuration)
    {
      this.model.Update(configuration);
      this.SetupDataContext();
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

    private void SetupDataContext()
    {
      if (this.model.OneGroup && string.IsNullOrWhiteSpace(this.model.FirstGroup.GroupName))
      {
        Binding myBinding = new Binding();
        myBinding.Source = this.model.FirstGroup;
        myBinding.Path = new PropertyPath("SubjectViewModels");
        myBinding.Mode = BindingMode.OneWay;
        myBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        BindingOperations.SetBinding(this.mainTreeView, TreeView.ItemsSourceProperty, myBinding);
      }
      else
      {
        Binding myBinding = new Binding();
        myBinding.Source = this.model;
        myBinding.Path = new PropertyPath("SubjectGroups");
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
