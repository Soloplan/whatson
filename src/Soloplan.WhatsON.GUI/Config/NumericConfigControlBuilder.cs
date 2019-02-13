// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NumericConfigControlBuilder.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config
{
  using System.Collections.Generic;
  using System.Text.RegularExpressions;
  using System.Windows;
  using System.Windows.Controls;

  public class NumericConfigControlBuilder : IConfigControlBuilder
  {
    public Control GetControl(KeyValuePair<string, string> configItem, ConfigurationItemAttribute configItemAttribute)
    {
      var textBox = new TextBox();
      textBox.Tag = configItemAttribute.Key;
      textBox.PreviewTextInput += this.NumberValidationTextBox;
      textBox.Text = configItem.Value;
      textBox.Width = 150;
      textBox.HorizontalAlignment = HorizontalAlignment.Left;
      return textBox;
    }

    private void NumberValidationTextBox(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
      var regex = new Regex("[^0-9]+");
      e.Handled = regex.IsMatch(e.Text);
    }
  }
}