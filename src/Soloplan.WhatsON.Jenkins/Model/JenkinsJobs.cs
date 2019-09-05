// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsJobs.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.Model
{
  using System.Collections.Generic;

  /// <summary>
  /// Represents the list of jobs.
  /// </summary>
  public class JenkinsJobs
  {
    /// <summary>
    /// The definition of properties used as query parameters.
    /// </summary>
    public const string RequestProperties = "jobs[url,name,fullName,displayName,description]";

    /// <summary>
    /// Gets or sets the jobs list.
    /// </summary>
    public IList<JenkinsJob> Jobs { get; set; }
  }
}