namespace Soloplan.WhatsON.ServerHealth
{
  using System;
  using System.Collections.Generic;

  public class ServerHealthPlugin : SubjectPlugin
  {
    public ServerHealthPlugin()
      : base(typeof(ServerHealth))
    {
    }

    public override Subject CreateNew(string name, IDictionary<string, string> configuration)
    {
      var address = configuration[ServerSubject.ServerAddress];
      if (string.IsNullOrWhiteSpace(address))
      {
        return null;
      }

      return new ServerHealth(name, address);
    }
  }
}