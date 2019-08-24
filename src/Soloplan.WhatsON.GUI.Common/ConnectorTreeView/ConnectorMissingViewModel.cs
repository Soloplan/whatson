// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConnectorMissingViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.ConnectorTreeView
{
  using System.Text;
  using System.Windows;
  using System.Windows.Input;
  using Microsoft.Expression.Interactivity.Core;
  using Soloplan.WhatsON.Configuration;

  public class ConnectorMissingViewModel : ConnectorViewModel
  {
    public ConnectorMissingViewModel()
    {
      void Act()
      {
        var builder = new StringBuilder();
        builder.AppendLine($"Identifier: {this.Identifier}");
        builder.AppendLine($"Name: {this.Name}");
        builder.AppendLine($"Description: {this.Description}");
        builder.AppendLine($"Expected plugin type: {this.ExpectedPluginType}");
        Clipboard.SetText(builder.ToString());
      }

      this.CopyData = new ActionCommand(Act);
    }

    /// <summary>
    /// Gets command for copying model data to clipboard.
    /// </summary>
    public ICommand CopyData { get; }

    /// <summary>
    /// Gets plugin type which was expected.
    /// </summary>
    public string ExpectedPluginType { get; private set; }

    /// <summary>
    /// Initializes viewmodel based on <paramref name="configuration"/>.
    /// </summary>
    /// <param name="configuration">Configuration of this connector.</param>
    public override void Init(ConnectorConfiguration configuration)
    {
      this.ExpectedPluginType = configuration.PluginTypeName;
      base.Init(configuration);
    }

    /// <summary>
    /// Doesn't do anything since <paramref name="changedConnector> is null.
    /// </summary>
    /// <param name="changedConnector">Connector which has changed - always null.</param>
    public override void Update(Connector changedConnector)
    {
      return;
    }
  }
}