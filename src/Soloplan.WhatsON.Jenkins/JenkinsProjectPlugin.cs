namespace Soloplan.WhatsON.Jenkins
{
  using System.Collections.Generic;

  public class JenkinsProjectPlugin : SubjectPlugin
  {
    public JenkinsProjectPlugin()
      : base(typeof(JenkinsProject))
    {
    }

    public override Subject CreateNew(string name, IList<ConfigurationItem> configuration)
    {
      var jenkinsProject = new JenkinsProject();
      jenkinsProject.Configuration = configuration;
      jenkinsProject.Name = name;
      return jenkinsProject;
    }
  }
}
