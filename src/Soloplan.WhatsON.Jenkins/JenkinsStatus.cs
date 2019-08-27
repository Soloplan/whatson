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
    public JenkinsStatus(ObservationState state)
    : base(state)
    {
    }

    public string DisplayName { get; set; }

    public TimeSpan EstimatedDuration { get; set; }

    public IList<JenkinsUser> CommittedToThisBuild { get; set; }

    public IList<JenkinsUser> Culprits { get; set; } = new List<JenkinsUser>();
  }
}