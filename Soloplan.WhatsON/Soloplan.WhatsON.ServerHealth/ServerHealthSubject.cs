namespace Soloplan.WhatsON.ServerHealth
{
  using System;
  using System.Net.NetworkInformation;
  using System.Text;

  public class ServerHealthSubject : ServerSubject
  {
    public ServerHealthSubject(string name, string serverAdress)
      : base(name)
    {
      this.Configuration[ServerAdress] = serverAdress;
    }

    protected override void ExecuteQuery(params string[] args)
    {
      var ping = new Ping();
      var options = new PingOptions();

      // Create a buffer of 32 bytes of data to be transmitted.
      var buffer = Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
      var reply = ping.Send(this.Adress, 120, buffer, options);

      var state = ObservationState.Unknown;
      if (reply?.Status == IPStatus.Success)
      {
        state = ObservationState.Success;
      }
      else if (reply != null)
      {
        state = ObservationState.Failed;
      }

      var newStatus = new Status(state) { Name = $"Pinging {this.Adress}", Time = DateTime.Now };
      Console.WriteLine(newStatus);

      this.CurrentStatus = newStatus;
    }
  }
}
