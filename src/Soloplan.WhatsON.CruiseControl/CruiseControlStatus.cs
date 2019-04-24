// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CruiseControlStatus.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.CruiseControl
{
  using System;
  using System.Collections.Generic;

  public class CruiseControlStatus : Status
  {
    public int BuildNumber { get; set; }

    public bool Building { get; set; }

    public bool Pending { get; set; }

    public bool CheckingModifications { get; set; }

    public TimeSpan Duration { get; set; }

    public TimeSpan EstimatedDuration { get; set; }

    public DateTime LastBuildTime { get; set; }

    public DateTime NextBuildTime { get; set; }

    public string JobUrl { get; set; }

    public IList<Culprit> Culprits { get; } = new List<Culprit>();
  }
}