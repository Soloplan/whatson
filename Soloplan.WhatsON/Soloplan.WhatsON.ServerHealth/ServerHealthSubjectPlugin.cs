namespace Soloplan.WhatsON.ServerHealth
{
  using System;
  using System.Collections.Generic;

  public class ServerHealthSubjectPlugin : SubjectPlugin
  {
    public ServerHealthSubjectPlugin()
      : base(typeof(ServerHealthSubject))
    {
    }

    public override Subject CreateNew(string name, IDictionary<string, string> configuration)
    {
      var address = configuration[ServerSubject.ServerAddress];
      if (string.IsNullOrWhiteSpace(address))
      {
        return null;
      }

      return new ServerHealthSubject(name, address);
    }
  }
}