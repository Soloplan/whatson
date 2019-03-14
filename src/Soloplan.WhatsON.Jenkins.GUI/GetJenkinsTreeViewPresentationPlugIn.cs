namespace Soloplan.WhatsON.Jenkins.GUI
{
  using System;
  using System.IO;
  using System.Reflection;
  using System.Xml;
  using Soloplan.WhatsON.GUI.SubjectTreeView;

  public class GetJenkinsTreeViewPresentationPlugIn : ITreeViewPresentationPlugIn
  {
    public Type SubjectType => typeof(JenkinsProject);

    public SubjectViewModel CreateViewModel()
    {
      return new JenkinsProjectViewModel();
    }

    public XmlReader GetDataTempletXaml()
    {
      string codeBase = Assembly.GetExecutingAssembly().CodeBase;
      UriBuilder uri = new UriBuilder(codeBase);
      string path = Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path));
      return XmlReader.Create(Path.Combine(path, "JenkinsProjectDataTemplate.xaml"));
    }
  }
}
