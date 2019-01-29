namespace Soloplan.WhatsON
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
      this.Configuration[ServerAddress] = address;
    }

    protected ServerSubject(string name, string address, string port)
      : this(name, address)
    {
      if (!string.IsNullOrWhiteSpace(port))
      {
        this.Configuration[ServerPort] = port;
      }
    }

    protected string Address => this.Configuration[ServerAddress];

    protected int Port
    {
      get
      {
        if (this.Configuration.TryGetValue(ServerPort, out var p))
        {
          return int.TryParse(p, out var port) ? port : 0;
        }

        return 0;
      }
    }
  }
}
