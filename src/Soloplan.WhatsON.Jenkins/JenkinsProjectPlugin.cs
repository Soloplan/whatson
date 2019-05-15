// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsProjectPlugin.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License.See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins
{
  using System;
  using System.Collections.Generic;
  using System.Threading.Tasks;
  using NLog;
  using Soloplan.WhatsON.Jenkins.Model;

  public class JenkinsProjectPlugin : SubjectPlugin, IProjectsListQuerying, IAssignServerProject
  {
    /// <summary>
    /// Gets a value indicating whether this plugin supports wizards.
    /// </summary>
    public override bool SupportsWizard => true;

    /// <summary>
    /// Logger instance used by this class.
    /// </summary>
    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

    public JenkinsProjectPlugin()
      : base(typeof(JenkinsProject))
    {
    }

    public override Subject CreateNew(SubjectConfiguration configuration)
    {
      log.Debug("Creating new JenkinsProject based on configuration {configuration}", new { Name = configuration.Name, Identifier = configuration.Identifier });
      var jenkinsProject = new JenkinsProject(configuration, new JenkinsApi());
      return jenkinsProject;
    }

    /// <summary>
    /// Gets the projects.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <returns>
    /// The projects list from the server.
    /// </returns>
    public async Task<IList<ServerProjectTreeItem>> GetProjects(string address)
    {
      var api = new JenkinsApi();
      var serverProjects = new List<ServerProjectTreeItem>();
      await this.GetProjectsLists(address, serverProjects, api);
      return serverProjects;
    }

    /// <summary>
    /// Assigns a server project to given configuration items.
    /// </summary>
    /// <param name="serverProject">The server project.</param>
    /// <param name="configurationItemsSupport">The configuration items provider.</param>
    /// <param name="serverAddress">The server address.</param>
    public void AssignServerProject(ServerProject serverProject, IConfigurationItemsSupport configurationItemsSupport, string serverAddress)
    {
      // for now, we extract the project name from the address
      var projectNameWithoutAddress = serverProject.Address.Substring(serverAddress.Length, serverProject.Address.Length - serverAddress.Length - 1).Trim('/');
      if (projectNameWithoutAddress.StartsWith("job", StringComparison.CurrentCultureIgnoreCase))
      {
        projectNameWithoutAddress = projectNameWithoutAddress.Substring(3, projectNameWithoutAddress.Length - 3).TrimStart('/');
      }

      configurationItemsSupport.GetConfigurationByKey(JenkinsProject.ProjectName).Value = projectNameWithoutAddress;
      configurationItemsSupport.GetConfigurationByKey(JenkinsProject.ServerAddress).Value = serverAddress;
    }

    /// <summary>
    /// Gets a project list from given jenkins server address.
    /// </summary>
    /// <param name="address">The server address.</param>
    /// <param name="serverProjects">The list of projects to update.</param>
    /// <param name="jenkinsApi">Jenkins Api instance.</param>
    /// <returns>A task representing the job.</returns>
    private async Task GetProjectsLists(string address, List<ServerProjectTreeItem> serverProjects, JenkinsApi jenkinsApi)
    {
      var jenkinsJobs = await jenkinsApi.GetJenkinsJobs(address, default(System.Threading.CancellationToken));
      foreach (var jenkinsJob in jenkinsJobs.Jobs)
      {
        var newServerProject = this.AddServerProject(serverProjects, jenkinsJob);
        if (string.Equals(jenkinsJob.ClassName.Trim(), "com.cloudbees.hudson.plugins.folder.Folder".Trim(), System.StringComparison.InvariantCultureIgnoreCase)
         || string.Equals(jenkinsJob.ClassName.Trim(), "org.jenkinsci.plugins.workflow.multibranch.WorkflowMultiBranchProject".Trim(), System.StringComparison.InvariantCultureIgnoreCase))
        {
          await this.GetProjectsLists(jenkinsJob.Url, newServerProject.ServerProjects, jenkinsApi);
        }
      }
    }

    /// <summary>
    /// Adds single server project to the server projects list.
    /// </summary>
    /// <param name="parentList">The parent list.</param>
    /// <param name="jenkinsJobsItem">The new Jenkins Job Item.</param>
    /// <returns>The newly  created server projects tree item.</returns>
    private ServerProjectTreeItem AddServerProject(IList<ServerProjectTreeItem> parentList, JenkinsJob jenkinsJobsItem)
    {
      var newServerProject = new ServerProjectTreeItem();
      newServerProject.Address = jenkinsJobsItem.Url;
      newServerProject.Name = jenkinsJobsItem.Name;
      parentList.Add(newServerProject);
      return newServerProject;
    }
  }
}
