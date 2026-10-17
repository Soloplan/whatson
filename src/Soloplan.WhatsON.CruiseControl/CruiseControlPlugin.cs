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
  using System.Net;
  using System.Net.Http;
  using System.Threading.Tasks;
  using System.Xml;
  using System.Xml.Serialization;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.CruiseControl.Model;
  using Soloplan.WhatsON.Model;

  public class CruiseControlPlugin : ConnectorPlugin
  {
    private static readonly HttpClient discoveryClient = new HttpClient();

    public CruiseControlPlugin()
      : base(typeof(CruiseControlConnector))
    {
    }

    public override Connector CreateNew(ConnectorConfiguration configuration, bool? checkRedirect = null) => new CruiseControlConnector(configuration);

    /// <summary>
    /// Gets the projects.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <returns>
    /// The projects from the server.
    /// </returns>
    public override async Task<IList<Project>?> GetProjects(string address)
    {
      var result = new List<Project>();
      using var response = await discoveryClient.GetAsync(CruiseControlServer.UrlHelper.GetXmlReportUrl(address));
      if (response.StatusCode == HttpStatusCode.NotFound)
      {
        return null;
      }

      response.EnsureSuccessStatusCode();
      using var stream = await response.Content.ReadAsStreamAsync();
      using var reader = XmlReader.Create(stream);
      reader.MoveToContent();
      if (reader.LocalName != "Projects" || reader.NamespaceURI.Length != 0)
      {
        return null;
      }

      var allProjects = (CruiseControlJobs?)new XmlSerializer(typeof(CruiseControlJobs)).Deserialize(reader);
      var serverProjects = new List<Project>();
      foreach (var project in allProjects?.CruiseControlProject ?? Array.Empty<CruiseControlJob>())
      {
        var serverProjectTreeItem = new Project(
          address,
          project.Name ?? string.Empty,
          address + "server/" + project.ServerName + "/project/" + project.Name + "/ViewProjectReport.aspx");
        if (string.IsNullOrWhiteSpace(project.ServerName))
        {
          result.Add(serverProjectTreeItem);
        }
        else
        {
          var serverProject = serverProjects.FirstOrDefault(s => s.Name == project.ServerName);
          if (serverProject == null)
          {
            serverProject = new Project(
              null,
              project.ServerName,
              address + "server/" + project.ServerName + "/viewServerReport.aspx",
              project.Name ?? string.Empty,
              address + "server/" + project.ServerName + "/project/" + project.Name);
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
    public override void Configure(Project project, IConfigurationItemProvider configurationItemsSupport, string? serverAddress = null)
    {
      configurationItemsSupport.GetConfigurationByKey(Connector.ProjectName).Value = project.Name;
      configurationItemsSupport.GetConfigurationByKey(Connector.ServerAddress).Value = project.Address;
      configurationItemsSupport.GetConfigurationByKey(Connector.DirectAddress).Value = project.DirectAddress;
    }
  }
}
