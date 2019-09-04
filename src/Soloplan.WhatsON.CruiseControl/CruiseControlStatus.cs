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
  using Soloplan.WhatsON.CruiseControl.Model;
  using Soloplan.WhatsON.Model;

  public class CruiseControlStatus : Status
  {
    public bool Pending { get; set; }

    public bool CheckingModifications { get; set; }

    public DateTime LastBuildTime { get; set; }

    public DateTime NextBuildTime { get; set; }

    public IList<CruiseControlUser> Culprits { get; } = new List<CruiseControlUser>();
  }
}