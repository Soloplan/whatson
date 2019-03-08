
namespace Soloplan.WhatsON.GUI.SubjectTreeView
{
  using System;
  using System.Linq;
  using System.Reflection;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Data;

  /// <summary>
  /// Interaction logic for SubjectsTreeView.xaml
  /// </summary>
  public partial class SubjectsTreeView : UserControl
  {
    public SubjectTreeViewModel model;
    public SubjectsTreeView()
    {
      InitializeComponent();
      var assembly = Assembly.LoadFrom("Soloplan.WhatsON.PluginGUIExtensions.dll");
      var providerType = assembly.GetType("Soloplan.WhatsON.PluginGUIExtensions.Jenkins.GetJenkinsGui");
      var getGui = Activator.CreateInstance(providerType) as IStatusGuiProvider;
      using (var resourceXml = getGui.GetDataTempletXaml())
      {
        var dictionary = System.Windows.Markup.XamlReader.Load(resourceXml) as ResourceDictionary;
        this.Resources.MergedDictionaries.Add(dictionary);
      }

    }

    public void Init(ObservationScheduler scheduler, Configuration configuration)
    {

      this.model = new SubjectTreeViewModel();
      this.model.Init(scheduler, configuration);
      //this.model.CountChanged += (s, e) => this.ExpandFirstNode();
      this.DataContext = this.model;
      //this.ExpandFirstNode();
    }

    //private void ExpandFirstNode()
    //{
    //  if (this.model.OneGroup)
    //  {
    //    this.mainTreeView.s
    //    foreach (var item in this.mainTreeView.Items.OfType<TreeViewItem>())
    //    {
    //      item.IsExpanded = true;
    //    }
    //  }
    //}

    //private void MainTreeView_Loaded(object sender, RoutedEventArgs e)
    //{
    //  ExpandFirstNode();
    //}
  }
}
