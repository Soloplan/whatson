// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsFailureApi.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins
{
  using System;
  using System.Collections.Generic;
  using System.Globalization;
  using System.IO;
  using System.Linq;
  using System.Net.Http;
  using System.Threading;
  using System.Threading.Tasks;
  using System.Xml;
  using System.Xml.Linq;
  using Newtonsoft.Json.Linq;

  public partial class JenkinsApi
  {
    public async Task<string?> GetFailedTests(JenkinsConnector connector, int buildNumber, CancellationToken token)
    {
      try
      {
        // Filter on the server so successful tests cannot crowd failures out of the response limit.
        var url = UrlHelper.BuildUrl(connector, buildNumber, false) + "/testReport/api/xml?tree=suites[cases[className,name,status]]&xpath="
          + Uri.EscapeDataString("(//case[status='FAILED' or status='REGRESSION'])[position()<=5]") + "&wrapper=failures";
        var body = await this.ReadFailureResponse(connector, url, "application/xml", token);
        if (body == null)
        {
          return null;
        }

        using var reader = XmlReader.Create(new StringReader(body), new XmlReaderSettings
        {
          DtdProcessing = DtdProcessing.Prohibit,
          XmlResolver = null,
          MaxCharactersInDocument = MaximumResponseBytes,
        });
        var report = XDocument.Load(reader);
        var names = (report.Root?.Name == "failures" ? report.Root.Elements("case") : Enumerable.Empty<XElement>())
          .Where(test => (string?)test.Element("status") is "FAILED" or "REGRESSION")
          .Select(test => string.Join(".", new[] { (string?)test.Element("className"), (string?)test.Element("name") }.Where(value => !string.IsNullOrWhiteSpace(value))))
          .Where(name => !string.IsNullOrWhiteSpace(name)).Distinct().Take(5);
        var summary = string.Join(Environment.NewLine, names);
        return string.IsNullOrEmpty(summary) ? null : summary;
      }
      catch (OperationCanceledException) when (token.IsCancellationRequested)
      {
        throw;
      }
      catch (Exception)
      {
        // Optional diagnostics must not hide the build status when a report is unavailable.
        return null;
      }
    }

    public async Task<string?> GetPipelineFailure(JenkinsConnector connector, int buildNumber, CancellationToken token)
    {
      try
      {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        var buildUrl = UrlHelper.BuildUrl(connector, buildNumber, false);
        var body = await this.ReadFailureResponse(connector, buildUrl + "/wfapi/describe", "application/json", timeout.Token);
        if (body == null)
        {
          return null;
        }

        var stages = JObject.Parse(body)["stages"]?.OfType<JObject>()
          .Where(stage => (string?)stage["status"] is "FAILED" or "UNSTABLE").Take(3) ?? Enumerable.Empty<JObject>();
        var lines = new List<string>();
        var inspectedNode = false;
        foreach (var stage in stages)
        {
          var name = (string?)stage["name"];
          if (string.IsNullOrWhiteSpace(name))
          {
            continue;
          }

          var message = (string?)stage["error"]?["message"];
          if (string.IsNullOrWhiteSpace(message) && (string?)stage["status"] == "FAILED" && !inspectedNode
              && uint.TryParse((string?)stage["id"], NumberStyles.None, CultureInfo.InvariantCulture, out var nodeId))
          {
            inspectedNode = true;
            var nodeBody = await this.ReadFailureResponse(connector, $"{buildUrl}/execution/node/{nodeId}/wfapi/describe", "application/json", timeout.Token);
            if (nodeBody != null)
            {
              message = JObject.Parse(nodeBody)["stageFlowNodes"]?.OfType<JObject>()
                .Where(node => (string?)node["status"] == "FAILED")
                .Select(node => (string?)node["error"]?["message"]).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
            }
          }

          lines.Add($"{name}: {(string.IsNullOrWhiteSpace(message) ? (string?)stage["status"] : message)}");
        }

        return lines.Count == 0 ? null : string.Join(Environment.NewLine, lines.Distinct());
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

    private async Task<string?> ReadFailureResponse(JenkinsConnector connector, string url, string mediaType, CancellationToken token)
    {
      using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
      timeout.CancelAfter(TimeSpan.FromSeconds(5));
      using var request = new HttpRequestMessage(HttpMethod.Get, url);
      this.Authenticate(request, connector);
      request.Headers.Accept.ParseAdd(mediaType);
      var client = request.Headers.Authorization == null ? pollingClient : authenticatedClient;
      using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
      if (!response.IsSuccessStatusCode || !string.Equals(response.Content.Headers.ContentType?.MediaType, mediaType, StringComparison.OrdinalIgnoreCase))
      {
        return null;
      }

      await response.Content.LoadIntoBufferAsync(MaximumResponseBytes, timeout.Token);
      return await response.Content.ReadAsStringAsync(timeout.Token);
    }
  }
}
