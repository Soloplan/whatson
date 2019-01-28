namespace Soloplan.WhatsON.ServerHealth
{
  using System;
  using System.Net.NetworkInformation;
  using System.Text;

  [SubjectType("Server Health Check", Description = "Subjects of this type ping a specified server and return a state that depends on the ping reply.")]
  public class ServerHealthSubject : ServerSubject
  {
    public ServerHealthSubject(string name, string serverAdress)
      : base(name, serverAdress)
    {
    }

    protected override void ExecuteQuery(params string[] args)
    {
      var ping = new Ping();
      var options = new PingOptions();

      // Create a buffer of 32 bytes of data to be transmitted.
      var buffer = Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

      try
      {
        var reply = ping.Send(this.Address, 120, buffer, options);

        var state = ObservationState.Unknown;
        if (reply?.Status == IPStatus.Success)
        {
          state = ObservationState.Success;
        }
        else if (reply != null)
        {
          state = ObservationState.Failed;
        }

        var newStatus = new Status(state) { Name = $"Pinging {this.Address}", Time = DateTime.Now };
        Console.WriteLine(newStatus);

        this.CurrentStatus = newStatus;
      }
      catch (PingException ex)
      {
        var newStatus = new Status(ObservationState.Failed) { Name = $"{this.Address}: {ex.Message}", Time = DateTime.Now, Detail = ex.InnerException?.Message };
        Console.WriteLine(newStatus);

        this.CurrentStatus = newStatus;
      }
    }
  }
}
