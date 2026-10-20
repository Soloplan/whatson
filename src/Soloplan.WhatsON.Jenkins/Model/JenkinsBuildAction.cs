// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsBuildAction.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.Model
{
  using System.Collections.Generic;

  public class JenkinsBuildAction
  {
    public int FailCount { get; set; }

    public IList<JenkinsFailureCause>? FoundFailureCauses { get; set; }
  }

}
