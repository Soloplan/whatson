// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Project.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Model
{
  using System.Collections.Generic;
  using Soloplan.WhatsON.Composition;

  /// <summary>
  /// Represents the metadata for a project on the server.
  /// </summary>
  public class Project
  {
    /// <summary>
    /// Gets or sets the name of the project.
    /// </summary>
    public string Name { get; set; }

    public string FullName { get; set; }

    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the address of the project.
    /// </summary>
    public string Address { get; set; }

    /// <summary>
    /// Gets or sets the plugin.
    /// </summary>
    public ConnectorPlugin Plugin { get; set; }

    /// <summary>
    /// Gets the server projects.
    /// </summary>
    public List<Project> Children { get; } = new List<Project>();
  }
}