
namespace Soloplan.WhatsON.GUI.SubjectTreeView
{
  using System;
  using System.ComponentModel;
  using System.Linq;
  using System.Reflection;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Data;
  using System.Windows.Markup;

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

    public void Init(ObservationScheduler scheduler, Configuration configuration)
    {
      this.model = new SubjectTreeViewModel();
      this.model.Init(scheduler, configuration);
      this.DataContext = this.model;
    }
  }
}
