// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsApi.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//    Licensed under the MIT License. See License-file in the project root for license information.
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
  using Soloplan.WhatsON.ServerBase;

  public class JenkinsApi : IJenkinsApi
  {
    /// <summary>
    /// Logger instance used by this class.
    /// </summary>
    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

    public async Task<JenkinsJob> GetJenkinsJob(JenkinsProject subject, CancellationToken token)
    {
      var address = subject.GetAddress();
      var projectName = subject.GetProject();

      var jobRequest = $"{address.Trim('/')}/job/{projectName.Trim('/')}/api/json?tree={JenkinsJob.RequestProperties}";
      log.Trace("Querying job: {jobRequest}", jobRequest);
      return await GetJenkinsModel<JenkinsJob>(subject, jobRequest, token);
    }

    public async Task<JenkinsBuild> GetJenkinsBuild(JenkinsProject subject, int buildNumber, CancellationToken token)
    {
      var address = subject.GetAddress();
      var projectName = subject.GetProject();

      var buildRequest = $"{address.Trim('/')}/job/{projectName.Trim('/')}/{buildNumber}/api/json?tree={JenkinsBuild.RequestProperties}";
      log.Trace("Querying build: {jobRequest}", buildRequest);
      return await GetJenkinsModel<JenkinsBuild>(subject, buildRequest, token);
    }

    private static async Task<TModel> GetJenkinsModel<TModel>(JenkinsProject subject, string requestUrl, CancellationToken token)
    where TModel : class
    {
      var request = WebRequest.Create(requestUrl);
      try
      {
        using (token.Register(() => request.Abort(), false))
        using (var response = await request.GetResponseAsync())
        {
          // Get the stream containing content returned by the server
          // Open the stream using a StreamReader for easy access
          using (var dataStream = response.GetResponseStream())
          using (var reader = new StreamReader(dataStream))
          {
            var responseFromServer = reader.ReadToEnd();
            return JsonConvert.DeserializeObject<TModel>(responseFromServer);
          }
        }
      }
      catch (WebException ex)
      {
        if (token.IsCancellationRequested)
        {
          throw new OperationCanceledException(ex.Message, ex, token);
        }

        throw;
      }
    }
  }
}