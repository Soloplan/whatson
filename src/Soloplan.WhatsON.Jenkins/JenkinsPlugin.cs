// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsPlugin.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins
{
  using System;
  using System.Collections.Concurrent;
  using System.Collections.Generic;
  using System.Net;
  using System.Net.Http;
  using System.Net.Mime;
  using System.Threading;
  using System.Threading.Tasks;
  using Newtonsoft.Json;
  using NLog;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Jenkins.Model;
  using Soloplan.WhatsON.Model;

  public class JenkinsPlugin : ConnectorPlugin
  {
    /// <summary>
    /// Logger instance used by this class.
    /// </summary>
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    private static readonly HttpClient discoveryClient = new HttpClient();

    private static readonly ConcurrentDictionary<string, bool> blueOceanCache = new ConcurrentDictionary<string, bool>();

    public JenkinsPlugin()
      : base(typeof(JenkinsConnector))
    {
    }

    public override Connector CreateNew(ConnectorConfiguration configuration, bool? checkRedirect = null)
      => this.CreateNewAsync(configuration, checkRedirect).GetAwaiter().GetResult();

    public override async Task<Connector> CreateNewAsync(ConnectorConfiguration configuration, bool? checkRedirect = null, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      log.Debug("Creating new JenkinsProject based on configuration {configuration}", new { configuration.Name, configuration.Identifier });
      var jenkinsProject = new JenkinsConnector(configuration, new JenkinsApi());

      if (checkRedirect != true || bool.TryParse(configuration.GetConfigurationByKey(JenkinsConnector.RedirectPlugin).Value, out _))
      {
        return jenkinsProject;
      }

      var address = jenkinsProject.Address;
      ArgumentException.ThrowIfNullOrWhiteSpace(address);
      if (blueOceanCache.TryGetValue(address, out var cached))
      {
        configuration.GetConfigurationByKey(JenkinsConnector.RedirectPlugin).Value = cached.ToString();
        return jenkinsProject;
      }

      var result = false;
      using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
      timeout.CancelAfter(TimeSpan.FromSeconds(10));
      try
      {
        using var response = await discoveryClient.GetAsync(
          JenkinsApi.UrlHelper.ProjectUrl(jenkinsProject) + JenkinsApi.UrlHelper.RedirectPluginUrlSuffix,
          timeout.Token).ConfigureAwait(false);
        result = response.IsSuccessStatusCode;
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
        throw;
      }
      catch (Exception exception)
      {
        log.Debug(exception, "Could not detect the optional Blue Ocean redirect.");
      }

      cancellationToken.ThrowIfCancellationRequested();
      configuration.GetConfigurationByKey(JenkinsConnector.RedirectPlugin).Value = result.ToString();
      blueOceanCache[address] = result;
      return jenkinsProject;
    }

    /// <summary>
    /// Gets the projects.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <returns>
    /// The projects list from the server.
    /// </returns>
    public override async Task<IList<Project>?> GetProjects(string address)
    {
      var serverProjects = new List<Project>();
      return await this.GetProjectsLists(address, serverProjects) ? serverProjects : null;
    }

    /// <summary>
    /// Assigns a server project to given configuration items.
    /// </summary>
    /// <param name="project">The server project.</param>
    /// <param name="configurationItemsSupport">The configuration items provider.</param>
    /// <param name="serverAddress">The server address.</param>
    public override void Configure(Project project, IConfigurationItemProvider configurationItemsSupport, string? serverAddress = null)
    {
      ArgumentException.ThrowIfNullOrWhiteSpace(serverAddress);
      var projectName = project.FullName;

      // hacky way to extract the project name from the URL for versions prior to 0.9.1...
      if (string.IsNullOrWhiteSpace(projectName))
      {
        // for now, we extract the project name from the address
        var projectAddress = project.Address;
        ArgumentException.ThrowIfNullOrWhiteSpace(projectAddress);
        var projectNameWithoutAddress = projectAddress.Substring(serverAddress.Length, projectAddress.Length - serverAddress.Length - 1).Trim('/');
        if (projectNameWithoutAddress.StartsWith(JenkinsApi.UrlHelper.JobUrlPrefix, StringComparison.CurrentCultureIgnoreCase))
        {
          projectName = projectNameWithoutAddress.Substring(3, projectNameWithoutAddress.Length - 3).TrimStart('/');
        }
      }

      configurationItemsSupport.GetConfigurationByKey(Connector.ProjectName).Value = projectName;
      configurationItemsSupport.GetConfigurationByKey(Connector.ServerAddress).Value = serverAddress;
    }

    /// <summary>
    /// Gets a project list from given jenkins server address.
    /// </summary>
    /// <param name="address">The server address.</param>
    /// <param name="projects">The list of projects to update.</param>
    /// <returns>A task representing the job.</returns>
    private async Task<bool> GetProjectsLists(string address, IList<Project> projects)
    {
      using var response = await discoveryClient.GetAsync(JenkinsApi.UrlHelper.JobsRequest(address));
      if (response.StatusCode == HttpStatusCode.NotFound)
      {
        return false;
      }

      response.EnsureSuccessStatusCode();
      if (!string.Equals(response.Content.Headers.ContentType?.MediaType, MediaTypeNames.Application.Json, StringComparison.OrdinalIgnoreCase))
      {
        return false;
      }

      var jenkinsJobs = JsonConvert.DeserializeObject<JenkinsJobs>(await response.Content.ReadAsStringAsync());
      if (jenkinsJobs?.Jobs == null)
      {
        return false;
      }

      return await this.AddProjects(jenkinsJobs.Jobs, projects);
    }

    private async Task<bool> AddProjects(IList<JenkinsJob> jobs, IList<Project> projects)
    {
      foreach (var jenkinsJob in jobs)
      {
        if (string.IsNullOrWhiteSpace(jenkinsJob.Url) || string.IsNullOrWhiteSpace(jenkinsJob.Name))
        {
          return false;
        }

        var newServerProject = this.AddProject(projects, jenkinsJob);
        if (jenkinsJob.Jobs != null || string.Equals(
              jenkinsJob.ClassName?.Trim(),
              "com.cloudbees.hudson.plugins.folder.Folder",
              StringComparison.InvariantCultureIgnoreCase) || string.Equals(
              jenkinsJob.ClassName?.Trim(),
              "org.jenkinsci.plugins.workflow.multibranch.WorkflowMultiBranchProject",
              StringComparison.InvariantCultureIgnoreCase))
        {
          var loaded = jenkinsJob.Jobs != null
            ? await this.AddProjects(jenkinsJob.Jobs, newServerProject.Children)
            : await this.GetProjectsLists(jenkinsJob.Url, newServerProject.Children);
          if (!loaded)
          {
            return false;
          }

          foreach (var child in newServerProject.Children)
          {
            child.Parent = newServerProject;
          }
        }
      }

      return true;
    }

    /// <summary>
    /// Adds single server project to the server projects list.
    /// </summary>
    /// <param name="parentList">The parent list.</param>
    /// <param name="jenkinsJobsItem">The new Jenkins Job Item.</param>
    /// <returns>The newly  created server projects tree item.</returns>
    private Project AddProject(IList<Project> parentList, JenkinsJob jenkinsJobsItem)
    {
      var newServerProject = new Project(
        jenkinsJobsItem.Url ?? throw new InvalidPlugInApiResponseException("Jenkins returned a job without a URL."),
        jenkinsJobsItem.DisplayName ?? jenkinsJobsItem.Name ?? throw new InvalidPlugInApiResponseException("Jenkins returned a job without a name."),
        jenkinsJobsItem.Url ?? throw new InvalidPlugInApiResponseException("Jenkins returned a job without a URL."),
        jenkinsJobsItem.FullName,
        jenkinsJobsItem.Description);
      parentList.Add(newServerProject);
      return newServerProject;
    }
  }
}
