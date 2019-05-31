// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsStatus.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License.See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins
{
  using System;
  using System.Collections.Generic;
  using System.Globalization;
  using Newtonsoft.Json;
  using Soloplan.WhatsON.Jenkins.Model;

  public class JenkinsStatus : Status
  {
    private const long TicksInMilisecond = 10000;

    public JenkinsStatus(ObservationState state)
    : base(state)
    {
    }

    public int BuildNumber
    {
      get
      {
        if (this.Properties.TryGetValue(BuildPropertyKeys.Number, out var buildNubmerString) && int.TryParse(buildNubmerString, out var buildNubmer))
        {
          return buildNubmer;
        }

        throw new InvalidOperationException($"{nameof(this.BuildNumber)} wasn't set.");
      }

      set
      {
        this.Properties[BuildPropertyKeys.Number] = value.ToString(CultureInfo.InvariantCulture);
      }
    }

    public bool Building
    {
      get
      {
        if (this.Properties.TryGetValue(BuildPropertyKeys.Building, out var buildingString) && bool.TryParse(buildingString, out var isBuilding))
        {
          return isBuilding;
        }

        throw new InvalidOperationException($"{nameof(this.Building)} wasn't set.");
      }

      set
      {
        this.Properties[BuildPropertyKeys.Building] = value.ToString(CultureInfo.InvariantCulture);
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

    public IList<Culprit> Culprits
    {
      get
      {
        var list = new List<Culprit>();
        if (this.Properties.TryGetValue(BuildPropertyKeys.Culprits, out var data))
        {
          foreach (var culprit in JsonConvert.DeserializeObject<IList<Culprit>>(data))
          {
            list.Add(culprit);
          }
        }

        return list.AsReadOnly();
      }

      set => this.Properties[BuildPropertyKeys.Culprits] = JsonConvert.SerializeObject(value);
    }

    private static class BuildPropertyKeys
    {
      public const string Number = "BuildNumber";
      public const string Building = "Building";
      public const string Duration = "Duration";
      public const string EstimatedDuration = "EstimatedDuration";
      public const string Culprits = "Culprits";
    }
  }
}