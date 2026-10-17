// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsPollingApi.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins
{
  using System;
  using System.Collections.Concurrent;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Net;
  using System.Net.Http;
  using System.Text;
  using System.Threading;
  using System.Threading.Tasks;
  using System.Xml;
  using System.Xml.Linq;
  using Newtonsoft.Json.Linq;
  using NLog;
  using Soloplan.WhatsON.Jenkins.Model;
  using Soloplan.WhatsON.Model;

  public partial class JenkinsApi
  {
    internal const int MaximumUrlBytes = 2048;
    internal const int MaximumResponseBytes = 2 * 1024 * 1024;
    internal const int MaximumJobsPerRequest = 25;
    internal const int MaximumRequestsPerPoll = 8;

    private static readonly Logger pollingLog = LogManager.GetCurrentClassLogger();
    private static readonly HttpClient pollingClient = new HttpClient(new HttpClientHandler
    {
      AutomaticDecompression = DecompressionMethods.All,
      AllowAutoRedirect = false,
      UseProxy = false,
      MaxResponseHeadersLength = 32,
    }) { Timeout = Timeout.InfiniteTimeSpan };

    private readonly ConcurrentDictionary<string, DateTimeOffset> compactUntil = new ConcurrentDictionary<string, DateTimeOffset>();

    public async Task<JenkinsPollingResult> GetPollingJobs(IReadOnlyCollection<JenkinsConnector> connectors, CancellationToken token)
    {
      var result = new JenkinsPollingResult();
      var requests = 0;
      var eligible = new List<JenkinsConnector>();
      foreach (var connector in connectors)
      {
        try
        {
          var single = new[] { connector };
          if (Encoding.UTF8.GetByteCount(PollingRequest(single, true).AbsoluteUri) > MaximumUrlBytes)
          {
            throw new PollingBatchTooLargeException();
          }

          eligible.Add(connector);
        }
        catch (Exception exception)
        {
          result.Failed.Add(connector);
          pollingLog.Warn(exception, "Jenkins polling URL is invalid or too long for project {project}", connector.Project);
        }
      }

      foreach (var batch in PlanPollingRequests(eligible))
      {
        if (requests >= MaximumRequestsPerPoll)
        {
          break;
        }

        var compact = this.compactUntil.TryGetValue(UrlHelper.ParentUrl(batch[0]), out var until) && until > DateTimeOffset.UtcNow;
        await Load(batch, compact);
      }

      return result;

      async Task Load(IReadOnlyList<JenkinsConnector> batch, bool compact)
      {
        token.ThrowIfCancellationRequested();
        if (requests >= MaximumRequestsPerPoll)
        {
          return;
        }

        try
        {
          var uri = PollingRequest(batch, compact);
          if (Encoding.UTF8.GetByteCount(uri.AbsoluteUri) > MaximumUrlBytes)
          {
            throw new PollingBatchTooLargeException();
          }

          foreach (var connector in batch)
          {
            result.Failed.Add(connector);
          }

          requests++;
          var jobs = await DownloadPollingJobs(uri, token);
          foreach (var connector in batch)
          {
            jobs.TryGetValue(UrlHelper.ProjectLeafName(connector), out var job);
            if (job != null)
            {
              job.DetailsOmitted = compact;
            }

            result.Jobs[connector] = job;
            result.Failed.Remove(connector);
          }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
          throw;
        }
        catch (PollingBatchTooLargeException)
        {
          if (!compact)
          {
            this.compactUntil[UrlHelper.ParentUrl(batch[0])] = DateTimeOffset.UtcNow.AddMinutes(5);
            await Load(batch, true);
          }
          else if (batch.Count > 1)
          {
            var middle = batch.Count / 2;
            await Load(batch.Take(middle).ToArray(), compact);
            await Load(batch.Skip(middle).ToArray(), compact);
          }
          else
          {
            result.Failed.Add(batch[0]);
            pollingLog.Warn("Jenkins polling limits exceeded for project {project}", batch[0].Project);
          }
        }
        catch (Exception exception)
        {
          foreach (var connector in batch)
          {
            result.Failed.Add(connector);
          }

          pollingLog.Warn(exception, "Jenkins polling batch failed for {count} projects", batch.Count);
        }
      }
    }

    internal static IEnumerable<IReadOnlyList<JenkinsConnector>> PlanPollingRequests(IReadOnlyCollection<JenkinsConnector> connectors)
    {
      var queues = connectors.GroupBy(UrlHelper.ParentUrl).ToDictionary(group => group.Key, group => new Queue<JenkinsConnector>(group));
      var planned = new HashSet<JenkinsConnector>();
      foreach (var connector in connectors)
      {
        if (planned.Contains(connector))
        {
          continue;
        }

        var queue = queues[UrlHelper.ParentUrl(connector)];
        var batch = new List<JenkinsConnector>();
        while (queue.Count > 0 && batch.Count < MaximumJobsPerRequest)
        {
          var next = queue.Peek();
          if (batch.Count > 0 && Encoding.UTF8.GetByteCount(PollingRequest(batch.Append(next).ToArray(), false).AbsoluteUri) > MaximumUrlBytes)
          {
            break;
          }

          batch.Add(queue.Dequeue());
          planned.Add(next);
        }

        yield return batch;
      }
    }

    internal static Uri PollingRequest(IReadOnlyList<JenkinsConnector> batch, bool compact)
    {
      var build = compact ? "number,building,result,timestamp,duration,estimatedDuration" : JenkinsBuild.RequestProperties;

      // Jenkins applies XPath after export: stay within one folder and a bounded build history.
      var tree = $"jobs[name,disabled,inQueue,lastBuild[{build}],builds[{build}]{{0,{Connector.MaxSnapshots + 1}}}]";
      var names = batch.Select(UrlHelper.ProjectLeafName).Distinct(StringComparer.Ordinal);
      var xpath = "/*/job[" + string.Join(" or ", names.Select(NamePredicate)) + "]";
      return new Uri(UrlHelper.ParentUrl(batch[0]) + "/api/xml?tree=" + Uri.EscapeDataString(tree)
        + "&xpath=" + Uri.EscapeDataString(xpath) + "&wrapper=jobs");
    }

    private static string NamePredicate(string name)
    {
      if (Encoding.UTF8.GetByteCount(name) <= 64)
      {
        return "name=" + XPathLiteral(name);
      }

      // Short filters avoid embedding enormous names; response lookup still requires the exact full name.
      var prefix = string.Concat(name.EnumerateRunes().Take(16));
      var suffix = string.Concat(name.EnumerateRunes().TakeLast(16));
      return "(starts-with(name," + XPathLiteral(prefix) + ") and contains(name," + XPathLiteral(suffix) + "))";
    }

    private static string XPathLiteral(string value) => !value.Contains('\'') ? "'" + value + "'"
      : !value.Contains('"') ? "\"" + value + "\""
      : "concat(" + string.Join(",\"'\",", value.Split('\'').Select(part => "'" + part + "'")) + ")";

    private static async Task<IDictionary<string, JenkinsJob>> DownloadPollingJobs(Uri uri, CancellationToken token)
    {
      using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
      timeout.CancelAfter(TimeSpan.FromSeconds(30));
      using var request = new HttpRequestMessage(HttpMethod.Get, uri);
      request.Headers.Accept.ParseAdd("application/xml");
      using var response = await pollingClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
      if ((int)response.StatusCode is 413 or 414 or 431)
      {
        throw new PollingBatchTooLargeException();
      }

      response.EnsureSuccessStatusCode();
      var mediaType = response.Content.Headers.ContentType?.MediaType;
      if (!string.Equals(mediaType, "application/xml", StringComparison.OrdinalIgnoreCase)
          && !string.Equals(mediaType, "text/xml", StringComparison.OrdinalIgnoreCase))
      {
        throw new InvalidPlugInApiResponseException("Jenkins polling did not return XML.");
      }

      if (response.Content.Headers.ContentLength > MaximumResponseBytes)
      {
        throw new PollingBatchTooLargeException();
      }

      await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
      using var body = new MemoryStream();
      var buffer = new byte[8192];
      int count;
      while ((count = await stream.ReadAsync(buffer, timeout.Token)) > 0)
      {
        if (body.Length + count > MaximumResponseBytes)
        {
          throw new PollingBatchTooLargeException();
        }

        body.Write(buffer, 0, count);
      }

      body.Position = 0;
      using var reader = XmlReader.Create(body, new XmlReaderSettings
      {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        MaxCharactersInDocument = MaximumResponseBytes,
      });
      var document = XDocument.Load(reader);
      if (document.Root?.Name != "jobs" || document.Root.Elements().Any(element => element.Name != "job"))
      {
        throw new InvalidPlugInApiResponseException("Jenkins polling returned an invalid jobs wrapper.");
      }

      return document.Root.Elements("job").Select(element => ToJson(element, 0).ToObject<JenkinsJob>() ?? throw new InvalidPlugInApiResponseException("Jenkins returned a null job.")).ToDictionary(job => job.Name ?? throw new InvalidPlugInApiResponseException("Jenkins returned a job without a name."), StringComparer.Ordinal);
    }

    private static JToken ToJson(XElement element, int depth)
    {
      if (depth > 32)
      {
        throw new InvalidPlugInApiResponseException("Jenkins polling XML is nested too deeply.");
      }

      if (!element.HasElements)
      {
        return new JValue(element.Value);
      }

      var result = new JObject();
      foreach (var group in element.Elements().GroupBy(child => child.Name.LocalName))
      {
        var arrayName = group.Key switch { "build" => "builds", "culprit" => "culprits", "changeSet" => "changeSets", "item" => "items", _ => null };
        result[arrayName ?? group.Key] = arrayName == null ? ToJson(group.Single(), depth + 1) : new JArray(group.Select(child => ToJson(child, depth + 1)));
      }

      return result;
    }

    private sealed class PollingBatchTooLargeException : Exception
    {
    }
  }
}
