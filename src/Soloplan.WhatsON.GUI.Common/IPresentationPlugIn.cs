// <copyright file="IPresentationPlugin.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.GUI.Common
{
  using System;
  using System.Xml;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;

  /// <summary>
  /// PlugIn which provides presentation of <see cref="ConnectorType"/> connectors.
  /// </summary>
  public interface IPresentationPlugin : IPlugin
  {
    /// <summary>
    /// Gets type of connector for which this PlugIn provides presentation.
    /// </summary>
    Type ConnectorType { get; }

    /// <summary>
    /// Creates <see cref="ConnectorViewModel"/> decedent.
    /// </summary>
    /// <returns><see cref="ConnectorViewModel"/> decedent.</returns>
    ConnectorViewModel CreateViewModel();

    /// <summary>
    /// Gets the XAML file defining DataTemplet for displaying view model created by <see cref="CreateViewModel"/>.
    /// </summary>
    /// <returns>Data template.</returns>
    XmlReader GetDataTempletXaml();
  }
}