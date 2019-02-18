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
  using System.Windows.Data;
  using MaterialDesignThemes.Wpf;
  using Soloplan.WhatsON.GUI.Config.ViewModel;

  public class TextConfigControlBuilder : IConfigControlBuilder
  {
    public virtual Control GetControl(ConfigurationItemViewModel configItem, ConfigurationItemAttribute configItemAttribute)
    {
      var textBox = new TextBox();
      textBox.Tag = configItemAttribute.Key;
      textBox.DataContext = configItem;
      var style = Application.Current.FindResource("MaterialDesignFloatingHintTextBox") as Style;
      textBox.Style = style;
      HintAssist.SetIsFloating(textBox, true);
      HintAssist.SetHint(textBox, configItemAttribute.Key);
      textBox.Margin = new Thickness(0, 0, 0, 8);

      var valueBinding = new Binding();
      valueBinding.Source = configItem;
      valueBinding.Path = new PropertyPath(nameof(ConfigurationItemViewModel.Value));
      valueBinding.Mode = BindingMode.TwoWay;
      valueBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
      BindingOperations.SetBinding(textBox, TextBox.TextProperty, valueBinding);
      return textBox;
    }
  }
}