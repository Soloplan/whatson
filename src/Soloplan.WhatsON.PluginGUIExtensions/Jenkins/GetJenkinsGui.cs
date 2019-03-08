namespace Soloplan.WhatsON.PluginGUIExtensions.Jenkins
{
  using System.Xml;
  using Soloplan.WhatsON.GUI.SubjectTreeView;

  class GetJenkinsGui : IStatusGuiProvider
  {
    public SubjectViewModel GetViewModel()
    {
      return new JenkinsProjectViewModel();
    }

    public XmlReader GetDataTempletXaml()
    {
      return XmlReader.Create("Jenkins/JenkinsProjectDataTemplate.xaml");
    }
  }
}
