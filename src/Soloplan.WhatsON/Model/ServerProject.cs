// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ServerProject.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Model
{
  using System.Collections.Generic;
  using Soloplan.WhatsON.Composition;

  /// <summary>
  /// Represents the metadata for a project on the server.
  /// </summary>
  public class ServerProject
  {
    /// <summary>
    /// Gets or sets the name of the project.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the address of the project.
    /// </summary>
    public string Address { get; set; }

    /// <summary>
    /// Gets or sets the plugin.
    /// </summary>
    public IConnectorPlugin Plugin { get; set; }
  }

  /// <summary>
  /// Represents the metadata of the project on the server which can also contain sub projects.
  /// </summary>
  /// <seealso cref="Soloplan.WhatsON.ServerProject" />
  public class ServerProjectTreeItem : ServerProject
  {
    /// <summary>
    /// Gets the server projects.
    /// </summary>
    public List<ServerProjectTreeItem> ServerProjects { get; } = new List<ServerProjectTreeItem>();
  }
}