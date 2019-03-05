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

  public class NumericConfigControlBuilder : TextConfigControlBuilder
  {
    public override Control GetControl(ConfigurationItemViewModel configItem, ConfigurationItemAttribute configItemAttribute)
    {
      var numericTextBox = base.GetControl(configItem, configItemAttribute);
      numericTextBox.PreviewTextInput += this.NumberValidationTextBox;
      numericTextBox.Width = 150;
      numericTextBox.HorizontalAlignment = HorizontalAlignment.Left;
      return numericTextBox;
    }

    private void NumberValidationTextBox(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
      var regex = new Regex("[^0-9]+");
      e.Handled = regex.IsMatch(e.Text);
    }
  }
}