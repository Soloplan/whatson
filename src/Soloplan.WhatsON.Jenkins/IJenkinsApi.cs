// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IJenkinsApi.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins
{
  using System.Threading;
  using System.Threading.Tasks;
  using Soloplan.WhatsON.Jenkins.Model;

  /// <summary>
  /// Interface defining API for Jenkins.
  /// </summary>
  public interface IJenkinsApi
  {
    /// <summary>
    /// Gets information about Jenkins Job.
    /// </summary>
    /// <param name="subject">Job for which informations are retrieved.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>Information about Jenkins job. <seealso cref="JenkinsJob"/>.</returns>
    Task<JenkinsJob> GetJenkinsJob(JenkinsProject subject, CancellationToken token);

    /// <summary>
    /// Gets information about Jenkins build.
    /// </summary>
    /// <param name="subject">Job for which informations are retrieved.</param>
    /// <param name="buildNumber">Build number.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>Information about specific build. <seealso cref="JenkinsBuild"/>.</returns>
    Task<JenkinsBuild> GetJenkinsBuild(JenkinsProject subject, int buildNumber, CancellationToken token);
  }
}