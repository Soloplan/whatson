namespace Soloplan.WhatsON.Jenkins.GUI
{
  using System;
  using System.IO;  using System.Text;
  using System.Xml;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;

  public class GetJenkinsTreeViewPresentationPlugIn : ITreeViewPresentationPlugIn
  {
    public Type ConnectorType => typeof(JenkinsProject);

    public ConnectorViewModel CreateViewModel()
    {
      return new JenkinsProjectViewModel();
    }

    public XmlReader GetDataTempletXaml()
    {
      return XmlReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(Properties.Resources.JenkinsProjectDataTemplate)));
    }
  }
}
