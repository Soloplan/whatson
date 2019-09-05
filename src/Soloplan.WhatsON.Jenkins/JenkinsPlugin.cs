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

    public JenkinsPlugin()
      : base(typeof(JenkinsConnector))
    {
    }

    public override Connector CreateNew(ConnectorConfiguration configuration)
    {
      log.Debug("Creating new JenkinsProject based on configuration {configuration}", new { configuration.Name, configuration.Identifier });
      var jenkinsProject = new JenkinsConnector(configuration, new JenkinsApi());
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
      await this.GetProjectsLists(address, serverProjects, api);
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
      var jenkinsJobs = await jenkinsApi.GetJenkinsJobs(address, default);
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
      var newServerProject = new Project { Address = jenkinsJobsItem.Url, Name = jenkinsJobsItem.DisplayName ?? jenkinsJobsItem.Name, FullName = jenkinsJobsItem.FullName, Description = jenkinsJobsItem.Description };
      parentList.Add(newServerProject);
      return newServerProject;
    }
  }
}
