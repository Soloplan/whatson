// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConnectionWizardPage.xaml.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration.Wizard
{
  using System.Windows.Controls;
  using System.Windows.Input;

  /// <summary>
  /// Interaction logic for ConnectionWizardPage.xaml.
  /// </summary>
  public partial class ConnectionWizardPage : Page
  {
    /// <summary>
    /// The wizard controller.
    /// </summary>
    private readonly WizardController wizardController;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionWizardPage"/> class.
    /// </summary>
    /// <param name="wizardController">The wizard controller.</param>
    public ConnectionWizardPage(WizardController wizardController)
    {
      this.wizardController = wizardController;
      this.InitializeComponent();
      this.DataContext = this;
      this.AutoDetectionCheckbox.DataContext = this.wizardController;

      this.AddressComboBox.Loaded += (s, e) =>
      {
        if (this.AddressComboBox.Items.Count > 0)
        {
          this.AddressComboBox.SelectedIndex = 0;
        }

        this.AddressComboBox.Focus();
      };
    }

    /// <summary>
    /// Handles the key up event for control which allows providing a project address.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="System.Windows.Input.KeyEventArgs"/> instance containing the event data.</param>
    private void ConnectionAdressKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
      if (e.Key == Key.Enter)
      {
        this.wizardController.GoToNextPage();
      }
    }
  }
}
