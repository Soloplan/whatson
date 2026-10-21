// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsAuthentication.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins
{
  using System;
  using System.Collections.Generic;
  using System.Net;
  using System.Net.Http;
  using System.Net.Http.Headers;
  using System.Text;
  using System.Threading;
  using System.Threading.Tasks;
  using Newtonsoft.Json;
  using Newtonsoft.Json.Linq;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Jenkins.Model;

  public partial class JenkinsApi
  {
    private static readonly HttpClient authenticatedClient = new HttpClient(new HttpClientHandler
    {
      AllowAutoRedirect = false,
      UseCookies = false,
      AutomaticDecompression = DecompressionMethods.All,
      UseProxy = false,
    }) { Timeout = TimeSpan.FromSeconds(30) };

    private readonly SecretProtector secrets;

    public JenkinsApi()
      : this(SecretProtector.Instance)
    {
    }

    internal JenkinsApi(SecretProtector secrets)
    {
      this.secrets = secrets;
    }

    public async Task<string> TestAuthentication(JenkinsConnector connector, CancellationToken token)
    {
      if (!connector.HasApiCredentials || connector.BatchAddress is not Uri address
        || (address.Scheme != Uri.UriSchemeHttps && address.Scheme != Uri.UriSchemeHttp)
        || !string.IsNullOrEmpty(address.UserInfo) || !string.IsNullOrEmpty(address.Query) || !string.IsNullOrEmpty(address.Fragment))
      {
        throw new InvalidOperationException("Configure a Jenkins address, user name and API token.");
      }

      var identity = await this.GetJson<JObject>(connector, new Uri(address, "whoAmI/api/json").AbsoluteUri, token);
      if (identity["authenticated"]?.Type != JTokenType.Boolean || identity["anonymous"]?.Type != JTokenType.Boolean
        || identity["name"]?.Type != JTokenType.String)
      {
        throw new InvalidPlugInApiResponseException("Jenkins did not return an authentication identity.");
      }

      var name = identity.Value<string>("name");
      if (!identity.Value<bool>("authenticated") || identity.Value<bool>("anonymous") || string.IsNullOrWhiteSpace(name))
      {
        throw new HttpRequestException("Jenkins did not authenticate these credentials.", null, HttpStatusCode.Unauthorized);
      }

      return name;
    }

    public async Task<string> SetJobVolunteer(JenkinsConnector connector, int buildNumber, string? expectedOwner, string? owner, CancellationToken token)
    {
      if (!connector.HasApiCredentials)
      {
        throw new InvalidOperationException("Configure a Jenkins user name and API token before updating the volunteer panel.");
      }

      var url = UrlHelper.ProjectUrl(connector, false).TrimEnd('/');
      var original = await this.GetJobDescription(connector, url, token);
      var description = JenkinsVolunteerPanel.SetFix(original, buildNumber, expectedOwner, owner);
      if (description == (original ?? string.Empty))
      {
        return description;
      }

      using var request = new HttpRequestMessage(HttpMethod.Post, url + "/submitDescription");
      this.Authenticate(request, connector);
      request.Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["description"] = description });
      using var response = await authenticatedClient.SendAsync(request, token);
      if (response.StatusCode == HttpStatusCode.Forbidden)
      {
        throw new HttpRequestException("Updating the Fixes in progress panel requires Jenkins Job/Configure permission.", null, response.StatusCode);
      }

      if (response.StatusCode != HttpStatusCode.Found)
      {
        response.EnsureSuccessStatusCode();
      }

      // shortcut: Jenkins description updates have no compare-and-swap; simultaneous writers can still conflict.
      var saved = await this.GetJobDescription(connector, url, token);
      if (saved != description)
      {
        throw new InvalidOperationException("Jenkins did not retain the Fixes in progress panel. Refresh the project and try again.");
      }

      return description;
    }

    public async Task<JenkinsBuild> SetVolunteer(JenkinsConnector connector, int buildNumber, string? expectedOwner, bool claim, CancellationToken token)
    {
      if (!connector.HasApiCredentials || buildNumber <= 0)
      {
        throw new InvalidOperationException("Configure a Jenkins user name and API token before volunteering.");
      }

      var url = UrlHelper.BuildRequest(connector, buildNumber);
      JenkinsBuild build;
      try
      {
        build = await this.GetJson<JenkinsBuild>(connector, url, token);
      }
      catch (HttpRequestException exception) when (!claim && exception.StatusCode == HttpStatusCode.NotFound)
      {
        // A retained job assignment can outlive its build after Jenkins discards old runs.
        return new JenkinsBuild { Number = buildNumber };
      }

      if (build.Number != buildNumber)
      {
        throw new InvalidPlugInApiResponseException("Jenkins returned a different build.");
      }

      var owner = JenkinsVolunteer.GetValidatedOwner(build.Description);
      if ((owner != null || claim) && (!string.Equals(owner, expectedOwner, StringComparison.Ordinal) || (claim && owner != null)))
      {
        throw new InvalidOperationException("The volunteer has changed. Refresh the build before trying again.");
      }

      if (claim && (build.Building || (build.Result != "FAILURE" && build.Result != "UNSTABLE")))
      {
        throw new InvalidOperationException("Only completed failed or unstable builds can be claimed.");
      }

      if (!claim && owner == null)
      {
        return build;
      }

      var description = JenkinsVolunteer.SetOwner(build.Description, claim ? connector.AuthenticationUser : null);
      using var request = new HttpRequestMessage(HttpMethod.Post, UrlHelper.BuildUrl(connector, buildNumber, false) + "/submitDescription");
      this.Authenticate(request, connector);
      request.Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["description"] = description });
      using var response = await authenticatedClient.SendAsync(request, token);
      if (response.StatusCode != HttpStatusCode.Found)
      {
        response.EnsureSuccessStatusCode();
      }

      // shortcut: Jenkins has no conditional description update; simultaneous writers can still overwrite each other.
      var saved = await this.GetJson<JenkinsBuild>(connector, url, token);
      if (saved.Number != buildNumber || saved.Description != description)
      {
        throw new InvalidOperationException("Jenkins did not retain the volunteer change. Refresh the build and try again.");
      }

      return saved;
    }

    private async Task<string?> GetJobDescription(JenkinsConnector connector, string url, CancellationToken token)
    {
      var job = await this.GetJson<JObject>(connector, url + "/api/json?tree=description", token);
      if (!job.TryGetValue("description", StringComparison.OrdinalIgnoreCase, out var description)
          || description.Type is not (JTokenType.Null or JTokenType.String))
      {
        throw new InvalidPlugInApiResponseException("Jenkins did not return a job description.");
      }

      return description.Value<string>();
    }

    private async Task<T> GetJson<T>(JenkinsConnector connector, string url, CancellationToken token)
      where T : class
    {
      if (string.IsNullOrEmpty(connector.EncryptedApiToken) && string.IsNullOrWhiteSpace(connector.AuthenticationUser))
      {
        return await SerializationHelper.Instance.GetJsonModel<T>(url, token);
      }

      using var request = new HttpRequestMessage(HttpMethod.Get, url);
      this.Authenticate(request, connector);
      request.Headers.Accept.ParseAdd("application/json");
      using var response = await authenticatedClient.SendAsync(request, token);
      response.EnsureSuccessStatusCode();
      if (response.Content.Headers.ContentType?.MediaType != "application/json")
      {
        throw new InvalidPlugInApiResponseException("Jenkins did not return JSON.");
      }

      var json = await response.Content.ReadAsStringAsync(token);
      return JsonConvert.DeserializeObject<T>(json) ?? throw new InvalidPlugInApiResponseException("Jenkins returned an empty response.");
    }

    private void Authenticate(HttpRequestMessage request, JenkinsConnector connector)
    {
      if (string.IsNullOrEmpty(connector.EncryptedApiToken) && string.IsNullOrWhiteSpace(connector.AuthenticationUser))
      {
        return;
      }

      if (!connector.HasApiCredentials || connector.AuthenticationUser?.Contains(':') == true)
      {
        throw new InvalidOperationException("Configure both a valid Jenkins user name and API token.");
      }

      if (request.RequestUri is not Uri uri || (uri.Scheme != Uri.UriSchemeHttps && !uri.IsLoopback))
      {
        throw new InvalidOperationException("Use an HTTPS Jenkins address when configuring an API token.");
      }

      var apiToken = this.secrets.Decrypt(connector.EncryptedApiToken);
      if (string.IsNullOrWhiteSpace(apiToken))
      {
        throw new InvalidOperationException("The Jenkins API token is empty.");
      }

      request.Headers.Authorization = new AuthenticationHeaderValue(
        "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(connector.AuthenticationUser + ":" + apiToken)));
    }
  }
}
