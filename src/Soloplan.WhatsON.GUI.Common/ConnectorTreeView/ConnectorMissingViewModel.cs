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
  using Soloplan.WhatsON.Model;

  public class ConnectorMissingViewModel : ConnectorViewModel
  {
    public ConnectorMissingViewModel(Connector connector)
      : base(connector)
    {
      void Act()
      {
        var builder = new StringBuilder();
        builder.AppendLine($"Identifier: {this.Identifier}");
        builder.AppendLine($"Name: {this.Name}");
        builder.AppendLine($"Description: {this.Description}");
        builder.AppendLine($"Expected connector type: {this.ExpectedConnectorType}");
        Clipboard.SetText(builder.ToString());
      }

      this.CopyData = new ActionCommand(Act);
      this.ExpectedConnectorType = connector?.Configuration?.Type;
    }

    /// <summary>
    /// Gets command for copying model data to clipboard.
    /// </summary>
    public ICommand CopyData { get; }

    /// <summary>
    /// Gets plugin type which was expected.
    /// </summary>
    public string ExpectedConnectorType { get; }

    /// <summary>
    /// Doesn't do anything since <paramref name="changedConnector"/> is null.
    /// </summary>
    /// <param name="changedConnector">Connector which has changed - always null.</param>
    public override void Update(Connector changedConnector)
    {
    }
  }
}