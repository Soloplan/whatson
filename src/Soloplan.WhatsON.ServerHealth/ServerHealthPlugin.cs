namespace Soloplan.WhatsON.ServerHealth
{
  public class ServerHealthPlugin : SubjectPlugin
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
    public override Subject CreateNew(SubjectConfiguration configuration)
    {
      return new ServerHealth(configuration);
    }
  }
}