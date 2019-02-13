// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TextConfigControlBuilder.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config
{
  using System.Collections.Generic;
  using System.Windows;
  using System.Windows.Controls;
  using MaterialDesignThemes.Wpf;

  public class TextConfigControlBuilder : IConfigControlBuilder
  {
    public virtual Control GetControl(KeyValuePair<string, string> configItem, ConfigurationItemAttribute configItemAttribute)
    {
      var textBox = new TextBox();
      textBox.Tag = configItemAttribute.Key;
      textBox.Text = configItem.Value;
      var style = Application.Current.FindResource("MaterialDesignFloatingHintTextBox") as Style;
      textBox.Style = style;
      HintAssist.SetIsFloating(textBox, true);
      HintAssist.SetHint(textBox, configItemAttribute.Key);
      textBox.Margin = new Thickness(0, 0, 0, 8);
      return textBox;
    }
  }
}