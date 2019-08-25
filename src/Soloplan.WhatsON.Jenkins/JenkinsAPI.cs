// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsAPI.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins
{
  using System;
  using System.IO;
  using System.Net;
  using System.Threading;
  using System.Threading.Tasks;
  using Newtonsoft.Json;
  using NLog;
  using Soloplan.WhatsON.Jenkins.Model;

  public class JenkinsApi : IJenkinsApi
  {
    /// <summary>
    /// Logger instance used by this class.
    /// </summary>
    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

    public async Task<JenkinsJob> GetJenkinsJob(JenkinsConnector connector, CancellationToken token)
    {
      var address = connector.Address;
      var projectName = connector.GetProject();

      var jobRequest = $"{address.Trim('/')}/job/{projectName.Trim('/')}/api/json?tree={JenkinsJob.RequestProperties}";
      log.Trace("Querying job: {jobRequest}", jobRequest);
      return await SerializationHelper.GetJsonModel<JenkinsJob>(jobRequest, token);
    }

    /// <summary>
    /// Gets the Jenkins jobs.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <param name="token">The token.</param>
    /// <returns>The task with a result representing the <see cref="JenkinsJobs"/>.</returns>
    public async Task<JenkinsJobs> GetJenkinsJobs(string address, CancellationToken token)
    {
      var jobsRequest = $"{address.Trim('/')}/api/json?tree={JenkinsJobs.RequestProperties}";
      log.Trace("Querying jobs: {jobsRequest}", jobsRequest);
      return await SerializationHelper.GetJsonModel<JenkinsJobs>(jobsRequest, token);
    }

    public async Task<JenkinsBuild> GetJenkinsBuild(JenkinsConnector connector, int buildNumber, CancellationToken token)
    {
      var address = connector.Address;
      var projectName = connector.GetProject();

      var buildRequest = $"{address.Trim('/')}/job/{projectName.Trim('/')}/{buildNumber}/api/json?tree={JenkinsBuild.RequestProperties}";
      log.Trace("Querying build: {jobRequest}", buildRequest);
      return await SerializationHelper.GetJsonModel<JenkinsBuild>(buildRequest, token);
    }
  }
}