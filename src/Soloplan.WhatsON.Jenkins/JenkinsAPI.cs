// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsAPI.cs" company="Soloplan GmbH">
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

  public class JenkinsApi : IJenkinsApi
  {
    public async Task<JenkinsJob> GetJenkinsJob(JenkinsConnector connector, CancellationToken token)
    {
      var jobRequest = UrlHelper.JobRequest(connector);
      return await SerializationHelper.GetJsonModel<JenkinsJob>(jobRequest, token);
    }

    public async Task<JenkinsJobs> GetJenkinsJobs(string address, CancellationToken token)
    {
      var jobsRequest = UrlHelper.JobsRequest(address);
      return await SerializationHelper.GetJsonModel<JenkinsJobs>(jobsRequest, token);
    }

    public async Task<JenkinsBuild> GetJenkinsBuild(JenkinsConnector connector, int buildNumber, CancellationToken token)
    {
      var buildRequest = UrlHelper.BuildRequest(connector, buildNumber);
      return await SerializationHelper.GetJsonModel<JenkinsBuild>(buildRequest, token);
    }

    public async Task<IList<JenkinsBuild>> GetBuilds(JenkinsConnector connector, CancellationToken token, int from = 0, int to = Connector.MaxSnapshots)
    {
      var buildsRequest = UrlHelper.BuildsRequest(connector, from, to);
      var builds = await SerializationHelper.GetJsonModel<JenkinsBuilds>(buildsRequest, token);

      return builds?.Builds ?? new List<JenkinsBuild>();
    }

    public static class UrlHelper
    {
      public const string RedirectPluginUrlSuffix = "/display/redirect";

      public static string JobsRequest(string address)
      {
        return $"{SanitizeBaseUrl(address)}/api/json?tree={JenkinsJobs.RequestProperties}";
      }

      public static string JobRequest(JenkinsConnector connector)
      {
        return $"{SanitizeBaseUrl(connector.Address)}/job/{connector.Project.Trim('/')}/api/json?tree={JenkinsJob.RequestProperties}";
      }

      public static string BuildUrl(JenkinsConnector connector, int buildNumber, bool respectRedirect = false)
      {
        var url = $"{SanitizeBaseUrl(connector.Address)}/job/{connector.Project.Trim('/')}/{buildNumber}";
        if (respectRedirect
           && bool.TryParse(connector.Configuration.GetConfigurationByKey(JenkinsConnector.RedirectPlugin)?.Value, out var redirect) && redirect)
        {
          return $"{url}{RedirectPluginUrlSuffix}";
        }

        return url;
      }

      public static string BuildRequest(JenkinsConnector connector, int buildNumber)
      {
        return $"{BuildUrl(connector, buildNumber)}/api/json?tree={JenkinsBuild.RequestProperties}";
      }

      public static string BuildsRequest(JenkinsConnector connector, int from, int to)
      {
        return $"{SanitizeBaseUrl(connector.Address)}/job/{connector.Project.Trim('/')}/api/json?tree=builds[{JenkinsBuild.RequestProperties}]{{{from},{to}}}";
      }

      private static string SanitizeBaseUrl(string address)
      {
        return address.TrimEnd('/');
      }
    }
  }
}