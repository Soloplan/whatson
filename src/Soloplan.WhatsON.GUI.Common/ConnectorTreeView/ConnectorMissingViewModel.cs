// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConnectorMissingViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.ConnectorTreeView
{
  using System.Windows;
  using System.Windows.Input;
  using Microsoft.Xaml.Behaviors.Core;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Model;

  public class ConnectorMissingViewModel : ConnectorViewModel
  {
    public ConnectorMissingViewModel(ConnectorConfiguration configuration)
    {
      this.Identifier = configuration.Identifier;
      this.Name = configuration.Name;
      this.ExpectedConnectorType = configuration.Type;

      void Act()
      {
        Clipboard.SetText(Localizer.MissingConnectorData(this.Identifier, this.Name, this.Description ?? string.Empty, this.ExpectedConnectorType ?? string.Empty));
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
    public string ExpectedConnectorType { get; }

    /// <summary>
    /// Doesn't do anything since <paramref name="changedConnector" /> is null.
    /// </summary>
    /// <param name="changedConnector">Connector which has changed - always null.</param>
    public override void Update(Connector? changedConnector)
    {
    }
  }
}
