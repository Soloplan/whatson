// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ServerHealtStatus.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License.See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.ServerHealth
{
  using System;

  /// <summary>
  /// Status of job pinging server.
  /// </summary>
  public class ServerHealthStatus : Status
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="ServerHealthStatus"/> class.
    /// </summary>
    public ServerHealthStatus()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerHealthStatus"/> class.
    /// </summary>
    /// <param name="state">The state of job.</param>
    public ServerHealthStatus(ObservationState state)
      : base(state)
    {
    }

    /// <summary>
    /// Gets or sets the time of last ping.
    /// </summary>
    public long? RoundtripTime { get; set; }

    /// <summary>
    /// Gets or sets time of last successful reply.
    /// </summary>
    public DateTime? LastSuccessReply { get; set; }
  }
}