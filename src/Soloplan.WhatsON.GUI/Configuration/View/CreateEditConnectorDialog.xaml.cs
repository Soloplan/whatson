// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CreateEditConnectorDialog.xaml.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration.View
{
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Data;
  using Soloplan.WhatsON.GUI.Configuration.ViewModel;

  /// <summary>
  /// Interaction logic for UserControl1.xaml.
  /// </summary>
  public partial class CreateEditConnectorDialog : UserControl
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateEditConnectorDialog" /> class.
    /// </summary>
    /// <param name="selectedConnector">The selected connector.</param>
    /// <param name="isNew">if set to <c>true</c> the connector is new.</param>
    public CreateEditConnectorDialog(ConnectorViewModel selectedConnector, bool isNew)
    {
      this.InitializeComponent();
      if (isNew && this.uxPluginType.Items.Count > 0)
      {
        this.uxPluginType.SelectedIndex = 0;
      }
      else
      {
        this.uxPluginType.SelectedItem = selectedConnector.SourceConnectorPlugin;
      }

      this.uxPluginType.IsEnabled = isNew;
    }

    /// <summary>
    /// OK button click.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
    private void OkButtonClick(object sender, RoutedEventArgs e)
    {
    }

    /// <summary>
    /// Cancel button click.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
    private void CancelButtonClick(object sender, RoutedEventArgs e)
    {
      foreach (var be in BindingOperations.GetSourceUpdatingBindings(this))
      {
        be.UpdateTarget();
      }
    }
  }
}
