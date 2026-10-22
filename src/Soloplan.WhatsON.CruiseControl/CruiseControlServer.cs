// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CruiseControlServer.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.CruiseControl
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Net;
  using System.Net.Http;
  using System.Threading;
  using System.Threading.Tasks;
  using System.Xml.Serialization;
  using HtmlAgilityPack;
  using NLog;
  using Soloplan.WhatsON.CruiseControl.Model;
  using Soloplan.WhatsON.Model;

  public class CruiseControlServer
  {
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    private static readonly HttpClient historyClient = new HttpClient();

    private readonly string baseUrl;

    private readonly SemaphoreSlim reportLock = new SemaphoreSlim(1, 1);

    private DateTime lastPolled;

    private CruiseControlJobs? cache;

    public CruiseControlServer(string address)
    {
      this.baseUrl = address.Trim('/');
    }

    public string ReportUrl => UrlHelper.GetXmlReportUrl(this.baseUrl);

    public async Task<CruiseControlJob?> GetProjectStatus(CancellationToken cancellationToken, string? projectName, int interval)
    {
      await this.reportLock.WaitAsync(cancellationToken);
      try
      {
        if (DateTime.UtcNow - this.lastPolled > TimeSpan.FromSeconds(interval))
        {
          log.Trace("Polling server {@server}", new { Address = this.ReportUrl, LastPolled = this.lastPolled, CallingProject = projectName });
          try
          {
            this.cache = await this.GetStatusAsync<CruiseControlJobs>(cancellationToken, this.ReportUrl);
          }
          catch (OperationCanceledException)
          {
            throw;
          }
          catch (Exception ex)
          {
            this.cache = null;
          }

          this.lastPolled = DateTime.UtcNow;
        }

        log.Trace("Retrieving value from cache for project {projectName}", projectName);
        return this.cache?.CruiseControlProject?.FirstOrDefault(job => job.Name == projectName);
      }
      finally
      {
        this.reportLock.Release();
      }
    }

    public async Task<List<CruiseControlBuild>> GetBuilds(string? projectName, int limit = Connector.MaxSnapshots, CancellationToken cancellationToken = default)
    {
      var history = new List<CruiseControlBuild>();

      ArgumentException.ThrowIfNullOrWhiteSpace(projectName);
      cancellationToken.ThrowIfCancellationRequested();
      var projectReportUrl = UrlHelper.GetAllBuildsUrl(this.baseUrl, projectName);
      try
      {
        var uri = new Uri(projectReportUrl);
        var html = uri.IsFile
          ? await File.ReadAllTextAsync(uri.LocalPath, cancellationToken).ConfigureAwait(false)
          : await historyClient.GetStringAsync(uri, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var recentBuilds = doc.DocumentNode.SelectNodes("//table[@class='RecentBuildsPanel']/tr/td/a[@class]");
        foreach (var b in (recentBuilds?.Take(limit) ?? Enumerable.Empty<HtmlNode>()))
        {
          var authority = uri.GetLeftPart(UriPartial.Authority);
          var build = CruiseControlBuild.FromHtmlNode(b, authority);
          if (build == null)
          {
            continue;
          }

          history.Add(build);
        }
      }
      catch (Exception ex) when (ex is HttpRequestException or IOException or UnauthorizedAccessException
        || (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested))
      {
        log.Error(ex);
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
        using (var response = await request.GetResponseAsync().WaitAsync(cancellationToken).ConfigureAwait(false))
        {
          // Get the stream containing content returned by the server
          // Open the stream using a StreamReader for easy access
          using (var dataStream = response.GetResponseStream())
          {
            using var buffer = new MemoryStream();
            await dataStream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            buffer.Position = 0;
            var xmlSerializer = new XmlSerializer(typeof(TModel));
            return xmlSerializer.Deserialize(buffer) as TModel
              ?? throw new InvalidPlugInApiResponseException("The XML response contains no model.");
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
      public static string GetReportUrl(string baseUrl, string? project = null) => $"{SanitizeBaseUri(baseUrl)}" + (project != null ? $"/project/{Uri.EscapeDataString(project)}/ViewProjectReport.aspx" : string.Empty);

      public static string GetXmlReportUrl(string baseUrl) => $"{SanitizeBaseUri(baseUrl)}/XmlStatusReport.aspx";

      public static string GetAllBuildsUrl(string baseUrl, string project) => $"{SanitizeBaseUri(baseUrl)}/project/{Uri.EscapeDataString(project)}/ViewAllBuilds.aspx";

      private static string SanitizeBaseUri(string baseUrl) => baseUrl.TrimEnd('/');
    }
  }
}
