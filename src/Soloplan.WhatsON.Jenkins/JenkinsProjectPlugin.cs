namespace Soloplan.WhatsON.Jenkins
{
  using System.Collections.Generic;
  using System.Linq;
  using Soloplan.WhatsON.ServerBase;

  public class JenkinsProjectPlugin : SubjectPlugin
  {
    public JenkinsProjectPlugin()
      : base(typeof(JenkinsProject))
    {
    }

    public override Subject CreateNew(string name, IList<ConfigurationItem> configuration)
    {
      var address = configuration.First(c => c.Key == ServerSubject.ServerAddress).Value;
      if (string.IsNullOrWhiteSpace(address))
      {
        return null;
      }

      var jobName = configuration.First(c => c.Key == JenkinsProject.ProjectName).Value;
      if (string.IsNullOrWhiteSpace(jobName))
      {
        return null;
      }

      string port = null;
      var serverPortConfiguration = configuration.FirstOrDefault(c => c.Key == ServerSubject.ServerPort);
      if (serverPortConfiguration != null)
      {
        port = serverPortConfiguration.Value;
      }

      var jenkinsProject = new JenkinsProject();
      jenkinsProject.Address = address;
      if (int.TryParse(port, out var newPort))
      {
        jenkinsProject.Port = newPort;
      }

      return jenkinsProject;
    }
  }
}
