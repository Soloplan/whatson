namespace Soloplan.WhatsON
{
  public static class ConnectorExtensions
  {
    public static string GetAddress(this ServerConnector connector)
    {
      return connector.ConnectorConfiguration.GetConfigurationByKey(ServerConnector.ServerAddress).Value;
    }
  }
}