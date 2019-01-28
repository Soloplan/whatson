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

    protected string Adress => this.Configuration[ServerAddress];

    protected int Port => int.TryParse(this.Configuration[ServerPort], out var port) ? port : 0;
  }
}
