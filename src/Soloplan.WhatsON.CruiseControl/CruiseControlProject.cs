// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CruiseControlProject.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.CruiseControl
{
  using System;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using Soloplan.WhatsON;
  using Soloplan.WhatsON.ServerBase;

  [SubjectType("Cruise Control Project Status", Description = "Retrieve the current status of a Cruise Control project.")]
  [ConfigurationItem(ProjectName, typeof(string), Optional = false, Priority = 300)]
  public class CruiseControlProject : ServerSubject
  {
    private const string ProjectName = "ProjectName";

    public CruiseControlProject(SubjectConfiguration configuration)
      : base(configuration)
    {
    }

    public string Project => this.SubjectConfiguration.GetConfigurationByKey(CruiseControlProject.ProjectName).Value;

    private ICruiseControlServerManagerPlugIn ServerManager
    {
      get
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

    /// <summary>
    /// Gets the project.
    /// </summary>
    /// <returns>Project name.</returns>
    public string GetProject()
    {
      return this.SubjectConfiguration.GetConfigurationByKey(CruiseControlProject.ProjectName).Value;
    }

    protected override async Task ExecuteQuery(CancellationToken cancellationToken, params string[] args)
    {
      var server = this.ServerManager.GetServer(this.Address);
      var projectData = await server.GetProjectStatus(cancellationToken, this.Project, 5);
    }
  }
}