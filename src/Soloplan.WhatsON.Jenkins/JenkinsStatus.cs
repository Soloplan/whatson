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
    private const long TicksInMilisecond = 10000;

    public JenkinsStatus(ObservationState state)
    : base(state)
    {
    }

    public string DisplayName
    {
      get
      {
        if (this.Properties.TryGetValue(BuildPropertyKeys.DisplayName, out var buildDisplayName))
        {
          return buildDisplayName;
        }

        return this.BuildNumber.ToString(CultureInfo.InvariantCulture);
      }

      set
      {
        this.Properties[BuildPropertyKeys.DisplayName] = value;
      }
    }

    public TimeSpan Duration
    {
      get => new TimeSpan(this.DurationInMs * TicksInMilisecond);
      set => this.DurationInMs = value.Ticks / 10000;
    }

    public long DurationInMs
    {
      get
      {
        if (this.Properties.TryGetValue(BuildPropertyKeys.Duration, out var durationString) && long.TryParse(durationString, out var durationInMs))
        {
          return durationInMs;
        }

        throw new InvalidOperationException($"{nameof(this.Duration)} wasn't set.");
      }

      set
      {
        this.Properties[BuildPropertyKeys.Duration] = value.ToString(CultureInfo.InvariantCulture);
      }
    }

    public TimeSpan EstimatedDuration
    {
      get => new TimeSpan(this.EstimatedDurationInMs * TicksInMilisecond);
      set => this.EstimatedDurationInMs = value.Ticks / 10000;
    }

    public long EstimatedDurationInMs
    {
      get
      {
        if (this.Properties.TryGetValue(BuildPropertyKeys.EstimatedDuration, out var durationString) && long.TryParse(durationString, out var durationInMs))
        {
          return durationInMs;
        }

        throw new InvalidOperationException($"{nameof(this.EstimatedDuration)} wasn't set.");
      }

      set
      {
        this.Properties[BuildPropertyKeys.EstimatedDuration] = value.ToString(CultureInfo.InvariantCulture);
      }
    }

    public IList<Culprit> CommittedToThisBuild { get; set; }

    public IList<Culprit> Culprits { get; set; } = new List<Culprit>();

    private static class BuildPropertyKeys
    {
      public const string Number = "BuildNumber";
      public const string DisplayName = "DisplayName";
      public const string Building = "Building";
      public const string Duration = "Duration";
      public const string EstimatedDuration = "EstimatedDuration";
      public const string Culprits = "Culprits";
    }
  }
}