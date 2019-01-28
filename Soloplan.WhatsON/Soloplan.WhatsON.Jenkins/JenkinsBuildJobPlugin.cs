
namespace Soloplan.WhatsON.Jenkins
{
  using System;
  using System.Collections.Generic;

  public class JenkinsBuildJobPlugin : SubjectPlugin
  {
    public JenkinsBuildJobPlugin()
      : base(typeof(JenkinsBuildJob))
    {
    }

    public override Subject CreateNew(string name, IDictionary<string, string> configuration)
    {
      var address = configuration[ServerSubject.ServerAddress];
      if (string.IsNullOrWhiteSpace(address))
      {
        return null;
      }

      var jobName = configuration[JenkinsBuildJob.JobName];
      if (string.IsNullOrWhiteSpace(jobName))
      {
        return null;
      }

      string port = null;
      if (configuration.TryGetValue(ServerSubject.ServerPort, out var p))
      {
        port = p;
      }

      return new JenkinsBuildJob(address, jobName, serverPort: port, name: name);
    }
  }
}
