namespace Soloplan.WhatsON.ServerHealth
{
  using System;
  using System.Net.NetworkInformation;
  using System.Text;
  using System.Threading;
  using System.Threading.Tasks;
  using Soloplan.WhatsON.ServerBase;

  [SubjectType("Server Health Check", Description = "Ping a server and return the state depending on the reply.")]
  [NotificationConfigurationItem(NotificationsVisbility, typeof(ConnectorNotificationConfiguration), SupportsRunningNotify = false, SupportsUnstableNotify = false, SupportsUnknownNotify = false, Priority = 1600000000)]
  public class ServerHealth : ServerSubject
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="ServerHealth"/> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    public ServerHealth(SubjectConfiguration configuration)
      : base(configuration)
    {
    }

    protected override async Task ExecuteQuery(CancellationToken cancellationToken, params string[] args)
    {
      var ping = new Ping();
      var options = new PingOptions();

      // Create a buffer of 32 bytes of data to be transmitted.
      var buffer = Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

      try
      {
        var reply = await ping.SendPingAsync(this.Address, 120, buffer, options);

        var state = ObservationState.Unknown;
        if (reply?.Status == IPStatus.Success)
        {
          state = ObservationState.Success;
        }
        else if (reply != null)
        {
          state = ObservationState.Failure;
        }

        var newStatus = new Status(state) { Name = $"Pinging {this.Address} ({reply?.RoundtripTime}ms)", Time = DateTime.Now };
        this.CurrentStatus = newStatus;
      }
      catch (PingException ex)
      {
        var newStatus = new Status(ObservationState.Failure) { Name = $"{this.Address}: {ex.Message}", Time = DateTime.Now, Detail = ex.InnerException?.Message };
        this.CurrentStatus = newStatus;
      }
    }
  }
}
