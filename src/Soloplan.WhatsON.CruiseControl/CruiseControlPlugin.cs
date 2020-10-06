// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CruiseControlPlugin.cs" company="Soloplan GmbH">
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
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.CruiseControl.Model;
  using Soloplan.WhatsON.Model;

  public class CruiseControlPlugin : ConnectorPlugin
  {
    public CruiseControlPlugin()
      : base(typeof(CruiseControlConnector))
    {
    }

    public override Connector CreateNew(ConnectorConfiguration configuration, bool? checkRedirect = null)
    {
      return new CruiseControlConnector(configuration);
    }

    /// <summary>
    /// Gets the projects.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <returns>
    /// The projects from the server.
    /// </returns>
    public override async Task<IList<Project>> GetProjects(string address)
    {
      var result = new List<Project>();
      var server = CruiseControlManager.GetServer(address, false);
      CruiseControlJobs allProjects = null;
      try
      {
        allProjects = await server.GetAllProjects();
      }
      catch (Exception ex)
      {
        throw ex;
      }

      if (allProjects == null)
      {
        throw new Exception();
      }

      var serverProjects = new List<Project>();
      foreach (var project in allProjects.CruiseControlProject)
      {
        var serverProjectTreeItem = new Project(address, project.Name, address + "server/" + project.ServerName + "/project/" + project.Name + "/ViewProjectReport.aspx");
        if (string.IsNullOrWhiteSpace(project.ServerName))
        {
          result.Add(serverProjectTreeItem);
        }
        else
        {
          var serverProject = serverProjects.FirstOrDefault(s => s.Name == project.ServerName);
          if (serverProject == null)
          {
            serverProject = new Project(null, project.ServerName, address + "server/" + project.ServerName + "/viewServerReport.aspx", project.Name, address + "server/" + project.ServerName + "/project/" + project.Name);
            serverProjects.Add(serverProject);
            result.Add(serverProject);
          }

          serverProject.Children.Add(serverProjectTreeItem);
          serverProjectTreeItem.Parent = serverProject;
        }
      }

      return result;
    }

    /// <summary>
    /// Assigns the <see cref="T:Soloplan.WhatsON.ServerProject" /> to <see cref="T:Soloplan.WhatsON.ConfigurationItem" />.
    /// </summary>
    /// <param name="project">The server project.</param>
    /// <param name="configurationItemsSupport">The configuration items provider.</param>
    /// <param name="serverAddress">The server address.</param>
    public override void Configure(Project project, IConfigurationItemProvider configurationItemsSupport, string serverAddress = null)
    {
      configurationItemsSupport.GetConfigurationByKey(Connector.ProjectName).Value = project.Name;
      configurationItemsSupport.GetConfigurationByKey(Connector.ServerAddress).Value = project.Address;
      configurationItemsSupport.GetConfigurationByKey(Connector.DirectAddress).Value = project.DirectAddress;
    }
  }
}