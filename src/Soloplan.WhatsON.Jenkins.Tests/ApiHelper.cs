// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ApiHelper.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.Tests
{
  using System;
  using Soloplan.WhatsON.Jenkins.Model;
  using Soloplan.WhatsON.Model;

  public static class ApiHelper
  {
    public static JenkinsBuild GetBuild(ObservationState state, int number)
    {
      return new JenkinsBuild
      {
        Number = number,
        Result = state.ToString(),
        Building = state == ObservationState.Running,
        Timestamp = (long)(new DateTime(2020, 1, 1, 12, number, 0, 0) - new DateTime(1970, 1, 1)).TotalMilliseconds,
      };
    }

    public static JenkinsJob GetProject(ObservationState state, int lastBuildNumber)
    {
      return new JenkinsJob
      {
        LastBuild = GetBuild(state, lastBuildNumber),
      };
    }
  }
}