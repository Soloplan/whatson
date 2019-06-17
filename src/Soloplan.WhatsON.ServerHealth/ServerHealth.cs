// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ServerHealth.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License.See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.ServerHealth
{
  using System;
  using System.Collections.Generic;
  using System.Globalization;
  using System.Linq;
  using System.Net.NetworkInformation;
  using System.Text;
  using System.Threading;
  using System.Threading.Tasks;
  using Soloplan.WhatsON.ServerBase;

  [ConnectorType("Server Health Check", Description = "Ping a server and return the state depending on the reply.")]
  [NotificationConfigurationItem(NotificationsVisbility, typeof(ConnectorNotificationConfiguration), SupportsRunningNotify = false, SupportsUnstableNotify = true, SupportsUnknownNotify = false, Priority = 1600000000)]
  [ConfigurationItem(AllowedFailedAttemptsProperty, typeof(int), Optional = false, Priority = 200, Caption = "Allowed failed attempts")]
  public class ServerHealth : ServerConnector
  {
    public const string AllowedFailedAttemptsProperty = "AllowedFailedAttempts";

    /// <summary>
    /// List of last few replies for server - used to show broken status only after multiple failed pings.
    /// </summary>
    private Queue<ServerHealthStatus> latestPingResults;

    /// <summary>
    /// Gets previous status.
    /// </summary>
    private Status prevStatus;

    /// <summary>
    /// The date of last successful reply.
    /// </summary>
    private DateTime? lastSuccessfulReply;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerHealth"/> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    public ServerHealth(ConnectorConfiguration configuration)
      : base(configuration)
    {
      this.latestPingResults = new Queue<ServerHealthStatus>();
    }

    /// <summary>
    /// Gets or sets the address.
    /// </summary>
    public int AllowedFailedAttempts
    {
      get => int.Parse(this.ConnectorConfiguration.GetConfigurationByKey(AllowedFailedAttemptsProperty).Value);
      set => this.ConnectorConfiguration.GetConfigurationByKey(AllowedFailedAttemptsProperty).Value = value.ToString(CultureInfo.InvariantCulture);
    }

    protected override async Task ExecuteQuery(CancellationToken cancellationToken, params string[] args)
    {
      var ping = new Ping();
      var options = new PingOptions();

      // Create a buffer of 32 bytes of data to be transmitted.
      var buffer = Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

      ServerHealthStatus status;
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

        status = new ServerHealthStatus(state) { Name = $"Pinging {this.Address} ({reply?.RoundtripTime}ms)", Time = DateTime.Now, RoundtripTime = reply?.RoundtripTime };
      }
      catch (PingException ex)
      {
        status = new ServerHealthStatus(ObservationState.Failure) { Name = $"{this.Address}: {ex.Message}", Time = DateTime.Now, Detail = ex.InnerException?.Message };
      }

      if (status.State == ObservationState.Success)
      {
        this.lastSuccessfulReply = status.Time;
      }

      this.latestPingResults.Enqueue(status);
      if (this.latestPingResults.Count > this.AllowedFailedAttempts)
      {
        this.latestPingResults.Dequeue();
      }

      var result = this.latestPingResults.Count(res => res.State == ObservationState.Failure);
      if (result == this.AllowedFailedAttempts || result == this.latestPingResults.Count || result == 0)
      {
        this.CurrentStatus = status;
      }
      else
      {
        var lastSuccess = this.latestPingResults.LastOrDefault(stat => stat.State == ObservationState.Success);
        this.CurrentStatus = new ServerHealthStatus
        {
          RoundtripTime = lastSuccess.RoundtripTime,
          Time = status.Time,
          Detail = lastSuccess.Detail,
          Name = lastSuccess.Name,
          State = ObservationState.Unstable,
        };
      }

      ((ServerHealthStatus)this.CurrentStatus).LastSuccessReply = this.lastSuccessfulReply;
    }

    protected override bool ShouldTakeSnapshot(Status status)
    {
      var takeSnapshot = this.prevStatus == null || this.prevStatus.State != this.CurrentStatus.State;
      this.prevStatus = this.CurrentStatus;
      return takeSnapshot;
    }
  }
}
