// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsAPI.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using Soloplan.WhatsON.Jenkins.Model;
  using Soloplan.WhatsON.Model;

  public class JenkinsApi : IJenkinsApi
  {
    public async Task<JenkinsJob> GetJenkinsJob(JenkinsConnector connector, CancellationToken token)
    {
      var jobRequest = UrlHelper.JobRequest(connector);
      try
      {
        return await SerializationHelper.Instance.GetJsonModel<JenkinsJob>(jobRequest, token);
      }
      catch (Exception)
      {
        return null;
      }
    }

    public async Task<JenkinsJobs> GetJenkinsJobs(string address, CancellationToken token)
    {
      var jobsRequest = UrlHelper.JobsRequest(address);
      try
      {
        return await SerializationHelper.Instance.GetJsonModel<JenkinsJobs>(jobsRequest, token);
      }
      catch (Exception ex)
      {
        throw ex;
      }
    }

    public async Task<JenkinsBuild> GetJenkinsBuild(JenkinsConnector connector, int buildNumber, CancellationToken token)
    {
      var buildRequest = UrlHelper.BuildRequest(connector, buildNumber);
      try
      {
        return await SerializationHelper.Instance.GetJsonModel<JenkinsBuild>(buildRequest, token);
      }
      catch (Exception)
      {
        return null;
      }
    }

    public async Task<IList<JenkinsBuild>> GetBuilds(JenkinsConnector connector, CancellationToken token, int from = 0, int to = Connector.MaxSnapshots)
    {
      var buildsRequest = UrlHelper.BuildsRequest(connector, from, to);
      JenkinsBuilds builds = null;
      try
      {
        builds = await SerializationHelper.Instance.GetJsonModel<JenkinsBuilds>(buildsRequest, token);
      }
      catch (Exception ex)
      {
      }

      return builds?.Builds ?? new List<JenkinsBuild>();
    }

    public static class UrlHelper
    {
      public const string RedirectPluginUrlSuffix = "/display/redirect";

      public const string JobUrlPrefix = "job";

      public static string JobsRequest(string address)
      {
        return $"{SanitizeBaseUrl(address)}/api/json?tree={JenkinsJobs.RequestProperties}";
      }

      public static string JobRequest(JenkinsConnector connector)
      {
        return $"{SanitizeBaseUrl(connector.Address)}/{GetRelativeProjectUrl(connector)}/api/json?tree={JenkinsJob.RequestProperties}";
      }

      public static string BuildUrl(JenkinsConnector connector, int buildNumber, bool appendRedirect = true)
      {
        var url = $"{SanitizeBaseUrl(connector.Address)}/{GetRelativeProjectUrl(connector)}/{buildNumber}";
        if (appendRedirect && bool.TryParse(connector.Configuration.GetConfigurationByKey(JenkinsConnector.RedirectPlugin)?.Value, out var redirect) && redirect)
        {
          return $"{url}{RedirectPluginUrlSuffix}";
        }

        return url;
      }

      public static string ProjectUrl(Connector connector, bool appendRedirect = true)
      {
        var url = $"{SanitizeBaseUrl(connector.Address)}/{GetRelativeProjectUrl(connector)}";

        if (bool.TryParse(connector.Configuration.GetConfigurationByKey(JenkinsConnector.RedirectPlugin)?.Value, out var redirect) && redirect)
        {
          return $"{url}{RedirectPluginUrlSuffix}";
        }

        return url;
      }

      public static string BuildRequest(JenkinsConnector connector, int buildNumber)
      {
        return $"{BuildUrl(connector, buildNumber, false)}/api/json?tree={JenkinsBuild.RequestProperties}";
      }

      public static string BuildsRequest(JenkinsConnector connector, int from, int to)
      {
        return $"{SanitizeBaseUrl(connector.Address)}/{GetRelativeProjectUrl(connector)}/api/json?tree=builds[{JenkinsBuild.RequestProperties}]{{{from},{to}}}";
      }

      private static string SanitizeBaseUrl(string address)
      {
        return address.TrimEnd('/');
      }

      private static string GetRelativeProjectUrl(Connector connector)
      {
        // support for legacy format connector project names: prior to 0.9.1 the connector project name was extracted from the URL and didn't reflect the actual name of the project (without URL aritfacts)
        if (connector.Project.Contains(JobUrlPrefix))
        {
          return $"{JobUrlPrefix}/{connector.Project}";
        }

        var parts = connector.Project.Split('/').Select(Uri.EscapeUriString);

        return $"{JobUrlPrefix}/{string.Join($"/{JobUrlPrefix}/", parts).TrimEnd('/')}";
      }
    }
  }
}