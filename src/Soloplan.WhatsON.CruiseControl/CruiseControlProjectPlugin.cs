// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CruiseControlProjectPlugin.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.CruiseControl
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading.Tasks;
  using Soloplan.WhatsON.ServerBase;

  public class CruiseControlProjectPlugin : SubjectPlugin, IProjectsListQuerying, IAssignServerProject
  {
    public CruiseControlProjectPlugin()
      : base(typeof(CruiseControlProject))
    {
    }

    /// <summary>
    /// Gets a value indicating whether this plugin supports wizards.
    /// </summary>
    public override bool SupportsWizard => true;

    public override Subject CreateNew(SubjectConfiguration configuration)
    {
      return new CruiseControlProject(configuration);
    }

    /// <summary>
    /// Gets the projects.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <returns>
    /// The projects from the server.
    /// </returns>
    public async Task<IList<ServerProjectTreeItem>> GetProjects(string address)
    {
      var result = new List<ServerProjectTreeItem>();
      var server = this.GetServerManager().GetServer(address, false);
      var allProjects = await server.GetAllProjects();
      var serverProjects = new List<ServerProjectTreeItem>();
      foreach (var project in allProjects.CruiseControlProject)
      {
        var serverProjectTreeItem = new ServerProjectTreeItem();
        serverProjectTreeItem.Name = project.Name;
        serverProjectTreeItem.Address = address;

        if (string.IsNullOrWhiteSpace(project.ServerName))
        {
          result.Add(serverProjectTreeItem);
        }
        else
        {
          var serverProject = serverProjects.FirstOrDefault(s => s.Name == project.ServerName);
          if (serverProject == null)
          {
            serverProject = new ServerProjectTreeItem();
            serverProject.Name = project.ServerName;
            serverProjects.Add(serverProject);
            result.Add(serverProject);
          }

          serverProject.ServerProjects.Add(serverProjectTreeItem);
        }
      }

      return result;
    }

    /// <summary>
    /// Assigns the <see cref="T:Soloplan.WhatsON.ServerProject" /> to <see cref="T:Soloplan.WhatsON.ConfigurationItem" />.
    /// </summary>
    /// <param name="serverProject">The server project.</param>
    /// <param name="configurationItemsSupport">The configuration items provider.</param>
    /// <param name="serverAddress">The server address.</param>
    public void AssignServerProject(ServerProject serverProject, IConfigurationItemsSupport configurationItemsSupport, string serverAddress)
    {
      configurationItemsSupport.GetConfigurationByKey(CruiseControlProject.ProjectName).Value = serverProject.Name;
      configurationItemsSupport.GetConfigurationByKey(ServerSubject.ServerAddress).Value = serverAddress;
    }

    /// <summary>
    /// Gets the server manager.
    /// </summary>
    /// <returns>The server manager.</returns>
    private ICruiseControlServerManagerPlugIn GetServerManager()
    {
      var serverManager = PluginsManager.Instance.PlugIns.OfType<ICruiseControlServerManagerPlugIn>().ToList();
      var pluginCount = serverManager.Count;
      if (pluginCount != 1)
      {
        if (pluginCount < 1)
        {
          throw new InvalidOperationException($"No plugin of type {typeof(ICruiseControlServerManagerPlugIn)} found.");
        }

        throw new InvalidOperationException($"More then one plugins of type {typeof(ICruiseControlServerManagerPlugIn)} found.");
      }

      return serverManager.FirstOrDefault();
    }
  }
}