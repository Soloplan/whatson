// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsPollingResult.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.Model
{
  using System.Collections.Generic;

  public sealed class JenkinsPollingResult
  {
    // A null job means confirmed missing; an absent key means it was not requested or failed.
    public IDictionary<JenkinsConnector, JenkinsJob?> Jobs { get; } = new Dictionary<JenkinsConnector, JenkinsJob?>();

    public ISet<JenkinsConnector> Failed { get; } = new HashSet<JenkinsConnector>();
  }
}
