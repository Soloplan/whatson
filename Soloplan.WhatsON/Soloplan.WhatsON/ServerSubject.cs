namespace Soloplan.WhatsON
{
  public abstract class ServerSubject : Subject
  {
    protected const string ServerAdress = "Address";
    protected const string ServerPort = "Port";

    protected ServerSubject(string name)
      : base(name)
    {
    }

    protected string Adress => this.Configuration[ServerAdress];

    protected int Port => int.TryParse(this.Configuration[ServerPort], out var port) ? port : 0;
  }
}
