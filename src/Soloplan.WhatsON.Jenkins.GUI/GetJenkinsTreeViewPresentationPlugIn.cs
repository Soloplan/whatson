namespace Soloplan.WhatsON.Jenkins.GUI
{
  using System;
  using System.IO;
  using System.Reflection;
  using System.Text;
  using System.Xml;
  using Soloplan.WhatsON.GUI.Common.SubjectTreeView;

  public class GetJenkinsTreeViewPresentationPlugIn : ITreeViewPresentationPlugIn
  {
    public Type SubjectType => typeof(JenkinsProject);

    public SubjectViewModel CreateViewModel()
    {
      return new JenkinsProjectViewModel();
    }

    public XmlReader GetDataTempletXaml()
    {
      return null;
    }
  }
}
