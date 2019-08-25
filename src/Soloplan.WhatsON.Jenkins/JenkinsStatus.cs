// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsStatus.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins
{
  using System;
  using System.Collections.Generic;
  using System.Globalization;
  using Newtonsoft.Json;
  using Soloplan.WhatsON.Jenkins.Model;
  using Soloplan.WhatsON.Model;

  public class JenkinsStatus : Status
  {
    private const long TicksInMillisecond = 10000;

    public JenkinsStatus(ObservationState state)
    : base(state)
    {
    }

    public string DisplayName { get; set; }

    public TimeSpan Duration
    {
      get => new TimeSpan(this.DurationInMs * TicksInMillisecond);
      set => this.DurationInMs = value.Ticks / 10000;
    }

    public long DurationInMs { get; set; }

    public TimeSpan EstimatedDuration
    {
      get => new TimeSpan(this.EstimatedDurationInMs * TicksInMillisecond);
      set => this.EstimatedDurationInMs = value.Ticks / 10000;
    }

    public long EstimatedDurationInMs { get; set; }

    public IList<Culprit> CommittedToThisBuild { get; set; }

    public IList<Culprit> Culprits { get; set; } = new List<Culprit>();
  }
}