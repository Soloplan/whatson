// <copyright file="PluginsManagerExtension.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.GUI.Common.ConnectorTreeView
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using Soloplan.WhatsON.Composition;

  /// <summary>
  /// Extension class for accessing plugIns of <see cref="ITreeViewPresentationPlugIn"/> type.
  /// </summary>
  public static class PluginManagerExtensions
  {
    /// <summary>
    /// Gets all found plugIns.
    /// </summary>
    /// <param name="manager">Plugin manager.</param>
    /// <returns>List of all <see cref="ITreeViewPresentationPlugIn"/>.</returns>
    public static IEnumerable<IPresentationPlugin> GetPresentationPlugins(this PluginManager manager)
    {
      return manager.PlugIns.OfType<IPresentationPlugin>();
    }

    /// <summary>
    /// Gets plugIn for appropriate for presenting <paramref name="connectorType"/>.
    /// </summary>
    /// <param name="manager">Plugin manager.</param>
    /// <param name="connectorType">Type of connector.</param>
    /// <returns>Appropriate <see cref="ITreeViewPresentationPlugIn"/>.</returns>
    public static IPresentationPlugin GetPresentationPlugin(this PluginManager manager, Type connectorType)
    {
      var allPlugins = manager.GetPresentationPlugins().ToList();
      var result = allPlugins.FirstOrDefault(plugIn => plugIn.ConnectorType.ToString() == connectorType.ToString());
      if (result != null)
      {
        return result;
      }

      return allPlugins.FirstOrDefault(plugIn => plugIn.ConnectorType.IsAssignableFrom(connectorType));
    }
  }
}