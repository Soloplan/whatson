// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsStatus.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins
{
  using System.Collections.Generic;
  using Soloplan.WhatsON.Jenkins.Model;
  using Soloplan.WhatsON.Model;

  public class JenkinsStatus : Status
  {
    public JenkinsStatus(ObservationState state)
      : base(state)
    {
    }

    public string? DisplayName { get; set; }

    public int SkippedTestCount { get; set; }

    public IReadOnlyList<JenkinsFix> ActiveFixes { get; set; } = System.Array.Empty<JenkinsFix>();

    public IList<JenkinsUser> CommittedToThisBuild { get; set; } = new List<JenkinsUser>();

    public IList<JenkinsUser> Culprits { get; set; } = new List<JenkinsUser>();
  }
}
