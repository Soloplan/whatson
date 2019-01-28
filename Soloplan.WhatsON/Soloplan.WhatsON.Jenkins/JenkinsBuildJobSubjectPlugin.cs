
namespace Soloplan.WhatsON.Jenkins
{
  using System;
  using System.Collections.Generic;

  public class JenkinsBuildJobSubjectPlugin : SubjectPlugin
  {
    public JenkinsBuildJobSubjectPlugin()
      : base(typeof(JenkinsBuildJobSubject))
    {
    }

    public override Subject CreateNew(string name, IDictionary<string, string> configuration)
    {
      var address = configuration[ServerSubject.ServerAddress];
      if (string.IsNullOrWhiteSpace(address))
      {
        return null;
      }

      var jobName = configuration[JenkinsBuildJobSubject.JobName];
      if (string.IsNullOrWhiteSpace(jobName))
      {
        return null;
      }

      string port = null;
      if (configuration.TryGetValue(ServerSubject.ServerPort, out var p))
      {
        port = p;
      }

      return new JenkinsBuildJobSubject(address, jobName, serverPort: port, name: name);
    }
  }
}
