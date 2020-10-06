// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IJenkinsApi.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins
{
  using System.Collections.Generic;
  using System.Threading;
  using System.Threading.Tasks;
  using Soloplan.WhatsON.Jenkins.Model;
  using Soloplan.WhatsON.Model;

  /// <summary>
  /// Interface defining API for Jenkins.
  /// </summary>
  public interface IJenkinsApi
  {
    /// <summary>
    /// Gets information about Jenkins Job.
    /// </summary>
    /// <param name="connector">Job for which informations are retrieved.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>Information about Jenkins job. <seealso cref="JenkinsJob"/>.</returns>
    Task<JenkinsJob> GetJenkinsJob(JenkinsConnector connector, CancellationToken token);

    /// <summary>
    /// Gets information about Jenkins build.
    /// </summary>
    /// <param name="connector">Job for which informations are retrieved.</param>
    /// <param name="buildNumber">Build number.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>Information about specific build. <seealso cref="JenkinsBuild"/>.</returns>
    Task<JenkinsBuild> GetJenkinsBuild(JenkinsConnector connector, int buildNumber, CancellationToken token);

    /// <summary>
    /// Gets information about Jenkins builds. Accesses all builds list between <paramref name="from"/> and <paramref name="to"/>.
    /// </summary>
    /// <param name="connector">Job for which informations are retrieved.</param>
    /// <param name="token">Cancellation token.</param>
    /// <param name="from">Start position in all builds list from where the information should be retrieved.</param>
    /// <param name="to">End position in all builds list to which the information should be retrivy.</param>
    /// <returns>Information of builds in all builds list with indexes <paramref name="from"/>, <paramref name="to"/>.</returns>
    Task<IList<JenkinsBuild>> GetBuilds(JenkinsConnector connector, CancellationToken token, int from = 0, int to = Connector.MaxSnapshots);
  }
}