namespace Soloplan.WhatsON.ServerHealth
{
  public class ServerHealthPlugin : ConnectorPlugin
  {
    public ServerHealthPlugin()
      : base(typeof(ServerHealth))
    {
    }

    /// <summary>
    /// Creates the new.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The new plugin.</returns>
    public override Connector CreateNew(ConnectorConfiguration configuration)
    {
      return new ServerHealth(configuration);
    }
  }
}