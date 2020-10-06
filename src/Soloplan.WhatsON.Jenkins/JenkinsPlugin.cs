// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsPlugin.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins
{
  using System;
  using System.Collections.Generic;
  using System.Threading.Tasks;
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
    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

    private static Dictionary<string, bool> blueOceanCache = new Dictionary<string, bool>();

    public JenkinsPlugin()
      : base(typeof(JenkinsConnector))
    {
    }

    public override Connector CreateNew(ConnectorConfiguration configuration, bool? checkRedirect = null)
    {
      log.Debug("Creating new JenkinsProject based on configuration {configuration}", new { configuration.Name, configuration.Identifier });
      var jenkinsProject = new JenkinsConnector(configuration, new JenkinsApi());

      if (checkRedirect == null)
      {
        return jenkinsProject;
      }
      else if (checkRedirect == false)
      {
        return jenkinsProject;
      }

      if (blueOceanCache.ContainsKey(jenkinsProject.Address))
      {
        jenkinsProject.Configuration.GetConfigurationByKey("RedirectPlugin").Value = blueOceanCache[jenkinsProject.Address].ToString();
        configuration.GetConfigurationByKey("RedirectPlugin").Value = blueOceanCache[jenkinsProject.Address].ToString();
        return jenkinsProject;
      }

      var task = Task.Run(async () => await jenkinsProject.IsReachableUrl(JenkinsApi.UrlHelper.ProjectUrl(jenkinsProject) + JenkinsApi.UrlHelper.RedirectPluginUrlSuffix));
      task.Wait();
      var result = task.Result;
      if (result == true)
      {
        jenkinsProject.Configuration.GetConfigurationByKey("RedirectPlugin").Value = "True";
        configuration.GetConfigurationByKey("RedirectPlugin").Value = "True";
        blueOceanCache.Add(jenkinsProject.Address, true);
      }
      else
      {
        blueOceanCache.Add(jenkinsProject.Address, false);
      }

      return jenkinsProject;
    }

    /// <summary>
    /// Gets the projects.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <returns>
    /// The projects list from the server.
    /// </returns>
    public override async Task<IList<Project>> GetProjects(string address)
    {
      var api = new JenkinsApi();
      var serverProjects = new List<Project>();
      try
      {
        await this.GetProjectsLists(address, serverProjects, api);
      }
      catch (Exception ex)
      {
        throw ex;
      }

      return serverProjects;
    }

    /// <summary>
    /// Assigns a server project to given configuration items.
    /// </summary>
    /// <param name="project">The server project.</param>
    /// <param name="configurationItemsSupport">The configuration items provider.</param>
    /// <param name="serverAddress">The server address.</param>
    public override void Configure(Project project, IConfigurationItemProvider configurationItemsSupport, string serverAddress)
    {
      var projectName = project.FullName;

      // hacky way to extract the project name from the URL for versions prior to 0.9.1...
      if (string.IsNullOrWhiteSpace(projectName))
      {
        // for now, we extract the project name from the address
        var projectNameWithoutAddress = project.Address.Substring(serverAddress.Length, project.Address.Length - serverAddress.Length - 1).Trim('/');
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
    /// <param name="jenkinsApi">Jenkins Api instance.</param>
    /// <returns>A task representing the job.</returns>
    private async Task GetProjectsLists(string address, IList<Project> projects, JenkinsApi jenkinsApi)
    {
      JenkinsJobs jenkinsJobs;
      try
      {
        jenkinsJobs = await jenkinsApi.GetJenkinsJobs(address, default);
      }
      catch (Exception ex)
      {
        throw ex;
      }

      if (jenkinsJobs == null)
      {
        return;
      }

      if (jenkinsJobs?.Jobs == null)
      {
        return;
      }

      foreach (var jenkinsJob in jenkinsJobs.Jobs)
      {
        var newServerProject = this.AddProject(projects, jenkinsJob);
        if (string.Equals(jenkinsJob.ClassName.Trim(), "com.cloudbees.hudson.plugins.folder.Folder".Trim(), System.StringComparison.InvariantCultureIgnoreCase)
         || string.Equals(jenkinsJob.ClassName.Trim(), "org.jenkinsci.plugins.workflow.multibranch.WorkflowMultiBranchProject".Trim(), System.StringComparison.InvariantCultureIgnoreCase))
        {
          await this.GetProjectsLists(jenkinsJob.Url, newServerProject.Children, jenkinsApi);
          foreach (var child in newServerProject.Children)
          {
            child.Parent = newServerProject;
          }
        }
      }
    }

    /// <summary>
    /// Adds single server project to the server projects list.
    /// </summary>
    /// <param name="parentList">The parent list.</param>
    /// <param name="jenkinsJobsItem">The new Jenkins Job Item.</param>
    /// <returns>The newly  created server projects tree item.</returns>
    private Project AddProject(IList<Project> parentList, JenkinsJob jenkinsJobsItem)
    {
      var newServerProject = new Project(jenkinsJobsItem.Url, jenkinsJobsItem.DisplayName ?? jenkinsJobsItem.Name, jenkinsJobsItem.Url, jenkinsJobsItem.FullName, jenkinsJobsItem.Description);
      parentList.Add(newServerProject);
      return newServerProject;
    }
  }
}
