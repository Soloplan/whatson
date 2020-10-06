// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CruiseControlServer.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.CruiseControl
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Net;
  using System.Threading;
  using System.Threading.Tasks;
  using System.Xml.Serialization;
  using HtmlAgilityPack;
  using NLog;
  using Soloplan.WhatsON.CruiseControl.Model;
  using Soloplan.WhatsON.Model;

  public class CruiseControlServer
  {
    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());
    private readonly string baseUrl;

    private DateTime lastPolled;

    private CruiseControlJobs cache;

    public CruiseControlServer(string address)
    {
      this.baseUrl = address.Trim('/');
    }

    public string ReportUrl => UrlHelper.GetXmlReportUrl(this.baseUrl);

    public async Task<CruiseControlJob> GetProjectStatus(CancellationToken cancellationToken, string projectName, int interval)
    {
      var pollInterval = new TimeSpan(0, 0, 0, interval, 0);
      if (DateTime.Now - this.lastPolled > pollInterval)
      {
        log.Trace("Polling server {@server}", new { Address = this.ReportUrl, LastPolled = this.lastPolled, CallingProject = projectName });
        this.lastPolled = DateTime.Now;
        try
        {
          this.cache = await this.GetStatusAsync<CruiseControlJobs>(cancellationToken, this.ReportUrl);
        }
        catch (Exception ex)
        {
          this.cache = null;
        }
      }

      log.Trace("Retrieving value from cache for project {projectName}", projectName);
      return this.cache?.CruiseControlProject?.FirstOrDefault(job => job.Name == projectName);
    }

    public async Task<List<CruiseControlBuild>> GetBuilds(string projectName, int limit = Connector.MaxSnapshots)
    {
      var history = new List<CruiseControlBuild>();

      var projectReportUrl = UrlHelper.GetAllBuildsUrl(this.baseUrl, projectName);
      using (var client = new WebClient())
      {
        try
        {
          var html = client.DownloadString(projectReportUrl);
          var doc = new HtmlDocument();
          doc.LoadHtml(html);
          var recentBuilds = doc.DocumentNode.SelectNodes("//table[@class='RecentBuildsPanel']/tr/td/a[@class]");
          foreach (var b in recentBuilds.Take(limit))
          {
            var authority = new Uri(projectReportUrl).GetLeftPart(System.UriPartial.Authority);
            var build = CruiseControlBuild.FromHtmlNode(b, authority);
            if (build == null)
            {
              continue;
            }

            history.Add(build);
          }
        }
        catch (WebException ex)
        {
          log.Error(ex);
        }
      }

      return history;
    }

    /// <summary>
    /// Gets all projects from the CC server.
    /// </summary>
    /// <returns>The list of all projects.</returns>
    public async Task<CruiseControlJobs> GetAllProjects()
    {
      try
      {
        return await this.GetStatusAsync<CruiseControlJobs>(default, this.ReportUrl);
      }
      catch (Exception ex)
      {
        throw ex;
      }
    }

    private async Task<TModel> GetStatusAsync<TModel>(CancellationToken cancellationToken, string requestUrl)
    where TModel : class
    {
      var request = WebRequest.Create(requestUrl);
      try
      {
        using (cancellationToken.Register(() => request.Abort(), false))
        using (var response = await request.GetResponseAsync())
        {
          // Get the stream containing content returned by the server
          // Open the stream using a StreamReader for easy access
          using (var dataStream = response.GetResponseStream())
          {
            var xmlSerializer = new XmlSerializer(typeof(TModel));
            return xmlSerializer.Deserialize(dataStream) as TModel;
          }
        }
      }
      catch (WebException ex)
      {
        if (cancellationToken.IsCancellationRequested)
        {
          throw new OperationCanceledException(ex.Message, ex, cancellationToken);
        }

        log.Error($"Could not fetch status from {requestUrl}.", ex);
        throw ex;
      }
    }

    public static class UrlHelper
    {
      public static string GetReportUrl(string baseUrl, string project = null)
      {
        return $"{SanitizeBaseUri(baseUrl)}" + (project != null ? $"/project/{Uri.EscapeDataString(project)}/ViewProjectReport.aspx" : string.Empty);
      }

      public static string GetXmlReportUrl(string baseUrl)
      {
        return $"{SanitizeBaseUri(baseUrl)}/XmlStatusReport.aspx";
      }

      public static string GetAllBuildsUrl(string baseUrl, string project)
      {
        return $"{SanitizeBaseUri(baseUrl)}/project/{Uri.EscapeDataString(project)}/ViewAllBuilds.aspx";
      }

      private static string SanitizeBaseUri(string baseUrl)
      {
        return baseUrl.TrimEnd('/');
      }
    }
  }
}