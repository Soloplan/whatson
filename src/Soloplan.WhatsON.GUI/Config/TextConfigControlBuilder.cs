// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TextConfigControlBuilder.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config
{
  using System.Collections.Generic;
  using System.Windows.Controls;

  public class TextConfigControlBuilder : IConfigControlBuilder
  {
    public Control GetControl(KeyValuePair<string, string> configItem, ConfigurationItemAttribute configItemAttribute)
    {
      var textBox = new TextBox();
      textBox.Tag = configItemAttribute.Key;
      textBox.Text = configItem.Value;
      return textBox;
    }
  }
}