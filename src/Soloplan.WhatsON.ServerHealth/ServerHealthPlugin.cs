namespace Soloplan.WhatsON.ServerHealth
{
  using System.Collections.Generic;

  public class ServerHealthPlugin : SubjectPlugin
  {
    public ServerHealthPlugin()
      : base(typeof(ServerHealth))
    {
    }

    public override Subject CreateNew(string name, IList<ConfigurationItem> configuration)
    {
      var newServerHealth = new ServerHealth();
      newServerHealth.Configuration = configuration;
      newServerHealth.Name = name;
      return newServerHealth;
    }
  }
}