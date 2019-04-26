// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IAssignServerProject.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON
{
  using System.Collections.Generic;

  /// <summary>
  /// The interface used to assign <see cref="ServerProject"/> to <see cref="ConfigurationItem"/>.
  /// </summary>
  public interface IAssignServerProject
  {
    /// <summary>
    /// Assigns the <see cref="ServerProject"/> to <see cref="ConfigurationItem"/>.
    /// </summary>
    /// <param name="serverProject">The server project.</param>
    /// <param name="configurationItems">The configuration items.</param>
    /// <param name="serverAddress">The server address.</param>
    void AssignServerProject(ServerProject serverProject, IList<IConfigurationItem> configurationItems, string serverAddress);
  }
}