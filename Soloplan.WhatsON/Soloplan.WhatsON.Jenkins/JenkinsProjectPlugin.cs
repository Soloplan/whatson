namespace Soloplan.WhatsON.Jenkins
{
  using System.Collections.Generic;

  public class JenkinsProjectPlugin : SubjectPlugin
  {
    public JenkinsProjectPlugin()
      : base(typeof(JenkinsProject))
    {
    }

    public override Subject CreateNew(string name, IDictionary<string, string> configuration)
    {
      var address = configuration[ServerSubject.ServerAddress];
      if (string.IsNullOrWhiteSpace(address))
      {
        return null;
      }

      var jobName = configuration[JenkinsProject.ProjectName];
      if (string.IsNullOrWhiteSpace(jobName))
      {
        return null;
      }

      string port = null;
      if (configuration.TryGetValue(ServerSubject.ServerPort, out var p))
      {
        port = p;
      }

      return new JenkinsProject(address, jobName, serverPort: port, name: name);
    }
  }
}
