// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ServerHealthStatusViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License.See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.ServerHealth.GUI
{
  using System;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;

  /// <summary>
  /// View model for displaying ping results.
  /// </summary>
  public class ServerHealthStatusViewModel : StatusViewModel
  {
    /// <summary>
    /// Backing field for <see cref="LastSuccessReplyTime"/>.
    /// </summary>
    private DateTime? lastSuccessReplyTime;

    /// <summary>
    /// Backing field for <see cref="RoundtripTime"/>.
    /// </summary>
    private long? roundtripTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerHealthStatusViewModel"/> class.
    /// </summary>
    /// <param name="connector">Connector used to initialize this instance.</param>
    public ServerHealthStatusViewModel(ConnectorViewModel connector)
      : base(connector)
    {
    }

    public override void Update(Status newStatus)
    {
      base.Update(newStatus);
      this.UpdateStateFlags();

      if (newStatus is ServerHealthStatus serverStatus)
      {
        this.LastSuccessReplyTime = serverStatus.LastSuccessReply;
        this.RoundtripTime = serverStatus.RoundtripTime;
      }

      this.OnPropertyChanged(nameof(this.LastReplySuccessful));
    }

    /// <summary>
    /// Gets or sets the last successful ping response time.
    /// </summary>
    public DateTime? LastSuccessReplyTime
    {
      get => this.lastSuccessReplyTime;
      set
      {
        if (this.lastSuccessReplyTime != value)
        {
          this.lastSuccessReplyTime = value;
          this.OnPropertyChanged();
        }
      }
    }

    /// <summary>
    /// Gets a value indicating whether last reply was successful.
    /// </summary>
    public bool LastReplySuccessful => this.Time == this.LastSuccessReplyTime;

    /// <summary>
    /// Gets or set the time of last ping.
    /// </summary>
    public long? RoundtripTime
    {
      get => this.roundtripTime;
      set
      {
        if (this.roundtripTime != value)
        {
          this.roundtripTime = value;
          this.OnPropertyChanged();
        }
      }
    }
  }
}
