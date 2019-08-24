// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IProjectsListQuerying.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Composition
{
  using System.Collections.Generic;
  using System.Threading.Tasks;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Model;

  /// <summary>
  /// Adds possibility to query project list and configure projects.
  /// </summary>
  public interface IProjectPlugin
  {
    /// <summary>
    /// Gets the projects.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <returns>The projects from the server.</returns>
    Task<IList<Project>> GetProjects(string address);

    /// <summary>
    /// Assigns the <see cref="Project"/> to <see cref="ConfigurationItem"/>.
    /// </summary>
    /// <param name="project">The server project.</param>
    /// <param name="configurationItemsSupport">The configuration items provider.</param>
    /// <param name="serverAddress">The server address.</param>
    void Configure(Project project, IConfigurationItemProvider configurationItemsSupport, string serverAddress);
  }
}