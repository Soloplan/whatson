// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IProjectsListQuerying.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON
{
  using System.Collections.Generic;
  using System.Threading.Tasks;

  /// <summary>
  /// Adds possibility to query projects list froma plugin.
  /// </summary>
  public interface IProjectsListQuerying
  {
    /// <summary>
    /// Gets the projects lists.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <returns>The projects list from the server.</returns>
    Task<IList<ServerProjectTreeItem>> GetProjectsLists(string address);
  }
}