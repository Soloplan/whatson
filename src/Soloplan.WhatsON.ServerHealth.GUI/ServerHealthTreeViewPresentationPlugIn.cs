namespace Soloplan.WhatsON.ServerHealth.GUI
{
  using System;
  using System.IO;
  using System.Reflection;
  using System.Text;
  using System.Xml;
  using Soloplan.WhatsON.GUI.SubjectTreeView;

  public class ServerHealthTreeViewPresentationPlugIn : ITreeViewPresentationPlugIn
  {
    public Type SubjectType => typeof(ServerHealth);

    public SubjectViewModel CreateViewModel()
    {
      return new ServerHealthViewModel();
    }

    public XmlReader GetDataTempletXaml()
    {
      string codeBase = Assembly.GetExecutingAssembly().CodeBase;
      UriBuilder uri = new UriBuilder(codeBase);
      string path = Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path));
      return XmlReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(Properties.Resources.ServerHealthDataTemplate)));
    }
  }
}
