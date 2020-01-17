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
    /// Initializes a new instance of the <see cref="Project"/> class.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <param name="name">The name.</param>
    /// <param name="fullName">The full name.</param>
    /// <param name="description">The description.</param>
    /// <param name="projectPlugIn">The project plug in.</param>
    /// <param name="parent">The parent.</param>
    public Project(string address, string name, string fullName = null, string description = null, ConnectorPlugin projectPlugIn = null, Project parent = null)
    {
      this.Address = address;
      this.Name = name;
      this.FullName = fullName;
      this.Description = description;
      this.Plugin = projectPlugIn;
      this.Parent = parent;
    }

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

    /// <summary>
    /// Gets or sets the parent.
    /// </summary>
    public Project Parent { get; set; }
  }
}