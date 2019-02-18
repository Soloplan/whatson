namespace Soloplan.WhatsON.ServerBase
{
  [ConfigurationItem(ServerAddress, typeof(string))]
  [ConfigurationItem(ServerPort, typeof(int))]
  public abstract class ServerSubject : Subject
  {
    public const string ServerAddress = "Address";
    public const string ServerPort = "Port";

    protected ServerSubject(string name)
      : base(name)
    {
    }

    protected ServerSubject(string name, string address)
      : this(name)
    {
      this.GetConfigurationByKey(ServerAddress).Value = address;
    }

    protected ServerSubject(string name, string address, string port)
      : this(name, address)
    {
      if (!string.IsNullOrWhiteSpace(port))
      {
        this.GetConfigurationByKey(ServerPort).Value = port;
      }
    }

    protected string Address => this.GetConfigurationByKey(ServerAddress).Value;

    protected int Port
    {
      get
      {
        var configItem = this.GetConfigurationByKey(ServerPort);
        if (configItem != null)
        {
          return int.TryParse(configItem.Value, out var port) ? port : 0;
        }

        return 0;
      }
    }
  }
}
