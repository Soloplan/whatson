namespace Soloplan.WhatsON.ServerHealth
{
  using System.Collections.Generic;
  using System.Linq;
  using Soloplan.WhatsON.ServerBase;

  public class ServerHealthPlugin : SubjectPlugin
  {
    public ServerHealthPlugin()
      : base(typeof(ServerHealth))
    {
    }

    public override Subject CreateNew(string name, IList<ConfigurationItem> configuration)
    {
      var address = configuration.First(c => c.Key == ServerSubject.ServerAddress).Value;
      if (string.IsNullOrWhiteSpace(address))
      {
        return null;
      }

      var newServerHealth = new ServerHealth();
      newServerHealth.Name = name;
      return newServerHealth;
    }
  }
}