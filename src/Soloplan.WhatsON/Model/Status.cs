// <copyright file="Status.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.Model
{
  using System;

  public class Status
  {
    public Status()
      : this(ObservationState.Unknown)
    {
    }

    public Status(ObservationState state)
    {
      this.State = state;
    }

    public virtual string Name { get; set; }

    public virtual string Label { get; set; }

    public virtual string Details { get; set; }

    public virtual DateTime Time { get; set; }

    public virtual TimeSpan Duration { get; set; }

    public virtual TimeSpan EstimatedDuration { get; set; }

    public virtual ObservationState State { get; set; }

    public virtual int BuildNumber { get; set; }

    public virtual bool InvalidBuild { get; set; }

    public virtual bool Building { get; set; }

    public virtual string Url { get; set; }

    public virtual string ErrorMessage { get; set; }
  }
}