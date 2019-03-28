// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NumericConfigControlBuilder.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config
{
  using System.Text.RegularExpressions;
  using System.Windows;
  using System.Windows.Controls;
  using Soloplan.WhatsON.GUI.Config.ViewModel;

  /// <summary>
  /// The numeric control builder with a numeric mask for input.
  /// </summary>
  /// <seealso cref="Soloplan.WhatsON.GUI.Config.TextConfigControlBuilder" />
  public class NumericConfigControlBuilder : TextConfigControlBuilder
  {
    /// <summary>
    /// Creates a new control and returns it.
    /// </summary>
    /// <param name="configItem">The configuration item of the subject.</param>
    /// <param name="configItemAttribute">The configuration item attribute.</param>
    /// <returns>
    /// Returns the <see cref="FrameworkElement" /> for the <see cref="configItem" />.
    /// </returns>
    public override FrameworkElement GetControlInternal(ConfigurationItemViewModel configItem, ConfigurationItemAttribute configItemAttribute)
    {
      var numericTextBox = base.GetControlInternal(configItem, configItemAttribute);
      numericTextBox.PreviewTextInput += this.NumberValidationTextBox;
      numericTextBox.Width = 150;
      numericTextBox.HorizontalAlignment = HorizontalAlignment.Left;
      return numericTextBox;
    }

    /// <summary>
    /// Numbers the validation text box.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="System.Windows.Input.TextCompositionEventArgs"/> instance containing the event data.</param>
    private void NumberValidationTextBox(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
      var regex = new Regex("[^0-9]+");
      e.Handled = regex.IsMatch(e.Text);
    }
  }
}