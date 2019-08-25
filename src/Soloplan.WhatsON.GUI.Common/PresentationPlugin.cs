// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PresentationPlugin.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common
{
  using System;
  using System.IO;
  using System.Text;
  using System.Xml;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;

  /// <summary>
  /// PlugIn which provides presentation of <see cref="ConnectorType"/> connectors.
  /// </summary>
  public abstract class PresentationPlugin : IPlugin
  {
    protected PresentationPlugin(string connectorType)
    {
      this.ConnectorType = connectorType;
    }

    protected PresentationPlugin(string connectorType, string layoutXaml)
    {
      this.ConnectorType = connectorType;
      this.LayoutXaml = layoutXaml;
    }

    /// <summary>
    /// Gets type of connector for which this PlugIn provides presentation.
    /// </summary>
    public string ConnectorType { get; }

    public string LayoutXaml { get; set; }

    /// <summary>
    /// Creates the <see cref="ConnectorViewModel"/> decedent.
    /// </summary>
    /// <returns><see cref="ConnectorViewModel"/> decedent.</returns>
    public abstract ConnectorViewModel CreateViewModel();

    /// <summary>
    /// Gets the XAML file defining the DataTemplet for displaying the view model created by <see cref="CreateViewModel"/>.
    /// </summary>
    /// <returns>Data template.</returns>
    public XmlReader GetDataTempletXaml()
    {
      if (string.IsNullOrWhiteSpace(this.LayoutXaml))
      {
        return null;
      }

      return XmlReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(this.LayoutXaml)));
    }
  }
}
