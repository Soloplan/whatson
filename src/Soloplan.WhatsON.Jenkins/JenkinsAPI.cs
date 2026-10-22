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

  public partial class JenkinsApi : IJenkinsApi
  {
    public async Task<JenkinsJob?> GetJenkinsJobAsync(JenkinsConnector connector, CancellationToken token)
    {
      var jobRequest = UrlHelper.JobRequest(connector, connector.Snapshots.Count == 0);
      try
      {
        return await this.GetJsonAsync<JenkinsJob>(connector, jobRequest, token);
      }
      catch (OperationCanceledException) when (token.IsCancellationRequested)
      {
        throw;
      }
      catch (Exception)
      {
        return null;
      }
    }

    public async Task<JenkinsJobs> GetJenkinsJobsAsync(string address, CancellationToken token)
    {
      var jobsRequest = UrlHelper.JobsRequest(address);
      try
      {
        return await SerializationHelper.Instance.GetJsonModelAsync<JenkinsJobs>(jobsRequest, token);
      }
      catch (Exception ex)
      {
        throw ex;
      }
    }

    public async Task<JenkinsBuild?> GetJenkinsBuildAsync(JenkinsConnector connector, int buildNumber, CancellationToken token)
    {
      var buildRequest = UrlHelper.BuildRequest(connector, buildNumber);
      try
      {
        return await this.GetJsonAsync<JenkinsBuild>(connector, buildRequest, token);
      }
      catch (Exception)
      {
        return null;
      }
    }

    public async Task<IList<JenkinsBuild>> GetBuildsAsync(
      JenkinsConnector connector,
      CancellationToken token,
      int from = 0,
      int to = Connector.MaxSnapshots)
    {
      var buildsRequest = UrlHelper.BuildsRequest(connector, from, to);
      JenkinsBuilds? builds = null;
      try
      {
        builds = await this.GetJsonAsync<JenkinsBuilds>(connector, buildsRequest, token);
      }
      catch (OperationCanceledException) when (token.IsCancellationRequested)
      {
        throw;
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

      public static string JobsRequest(string address) => $"{SanitizeBaseUrl(address)}/api/json?tree={JenkinsJobs.RequestProperties}";

      public static string JobRequest(JenkinsConnector connector) => JobRequest(connector, false);

      public static string JobRequest(JenkinsConnector connector, bool includeBuilds) =>
        $"{SanitizeBaseUrl(connector.Address)}/{GetRelativeProjectUrl(connector)}/api/json?tree={JenkinsJob.RequestProperties}"
        + (includeBuilds ? $",builds[{JenkinsBuild.RequestProperties}]{{0,{Connector.MaxSnapshots + 1}}}" : string.Empty);

      public static string BuildUrl(JenkinsConnector connector, int buildNumber, bool appendRedirect = true)
      {
        var url = $"{SanitizeBaseUrl(connector.Address)}/{GetRelativeProjectUrl(connector)}/{buildNumber}";
        if (appendRedirect && bool.TryParse(connector.Configuration.GetConfigurationByKey(JenkinsConnector.RedirectPlugin)?.Value, out var redirect)
                           && redirect)
        {
          return $"{url}{RedirectPluginUrlSuffix}";
        }

        return url;
      }

      public static string ProjectUrl(Connector connector, bool appendRedirect = true)
      {
        var url = $"{SanitizeBaseUrl(connector.Address)}/{GetRelativeProjectUrl(connector)}";

        if (appendRedirect && bool.TryParse(connector.Configuration.GetConfigurationByKey(JenkinsConnector.RedirectPlugin)?.Value, out var redirect)
                           && redirect)
        {
          return $"{url}{RedirectPluginUrlSuffix}";
        }

        return url;
      }

      public static string BuildRequest(JenkinsConnector connector, int buildNumber) => $"{BuildUrl(connector, buildNumber, false)}/api/json?tree={JenkinsBuild.RequestProperties}";

      public static string BuildsRequest(JenkinsConnector connector, int from, int to)
      {
        return
          $"{SanitizeBaseUrl(connector.Address)}/{GetRelativeProjectUrl(connector)}/api/json?tree=builds[{JenkinsBuild.RequestProperties}]{{{from},{to}}}";
      }

      internal static string ProjectFullName(Connector connector) => string.Join(
        "/", GetRelativeProjectUrl(connector).Split('/').Where((part, index) => index % 2 == 1).Select(Uri.UnescapeDataString));

      internal static string ParentUrl(Connector connector)
      {
        var path = GetRelativeProjectUrl(connector);
        var boundary = path.LastIndexOf("/job/", StringComparison.Ordinal);
        return SanitizeBaseUrl(connector.Address) + (boundary < 0 ? string.Empty : "/" + path.Substring(0, boundary));
      }

      internal static string ProjectLeafName(Connector connector) => Uri.UnescapeDataString(GetRelativeProjectUrl(connector).Split('/').Last());

      private static string SanitizeBaseUrl(string? address)
      {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        return address.TrimEnd('/');
      }

      private static string GetRelativeProjectUrl(Connector connector)
      {
        var project = connector.Project;
        ArgumentException.ThrowIfNullOrWhiteSpace(project);

        // support for legacy format connector project names: prior to 0.9.1 the connector project name was extracted from the URL and didn't reflect the actual name of the project (without URL aritfacts)
        if (project.Contains($"/{JobUrlPrefix}/"))
        {
          return $"{JobUrlPrefix}/{project}";
        }

        var parts = project.Split('/').Select(Uri.EscapeDataString);

        return $"{JobUrlPrefix}/{string.Join($"/{JobUrlPrefix}/", parts).TrimEnd('/')}";
      }
    }
  }
}
