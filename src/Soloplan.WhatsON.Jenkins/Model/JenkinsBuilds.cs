// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsBuilds.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.Model
{
  using System.Collections.Generic;

  public class JenkinsBuilds
  {
    public IList<JenkinsBuild> Builds { get; set; }
  }
}