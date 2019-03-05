// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TextWithComboBoxPreselectionConfigControlBuilder.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config
{
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Data;
  using MaterialDesignThemes.Wpf;
  using Soloplan.WhatsON.GUI.Config.ViewModel;

  /// <summary>
  /// The control builder for editable combo box with provided suggested values.
  /// </summary>
  /// <seealso cref="Soloplan.WhatsON.GUI.Config.IConfigControlBuilder" />
  public class ComboBoxConfigControlBuilder : IConfigControlBuilder
  {
    /// <summary>
    /// Gets the supported configuration items key.
    /// </summary>
    public virtual string SupportedConfigurationItemsKey => null;

    /// <summary>
    /// Creates a new control and returns it.
    /// </summary>
    /// <param name="configItem">The configuration item of the subject.</param>
    /// <param name="configItemAttribute">The configuration item attribute.</param>
    /// <returns>
    /// Returns the <see cref="Control" /> for the <see cref="configItem" />.
    /// </returns>
    public virtual Control GetControl(ConfigurationItemViewModel configItem, ConfigurationItemAttribute configItemAttribute)
    {
      var comboBox = new ComboBox();
      comboBox.DataContext = configItem;
      var style = Application.Current.FindResource("MaterialDesignFloatingHintComboBox") as Style;
      comboBox.Style = style;
      HintAssist.SetIsFloating(comboBox, true);
      HintAssist.SetHint(comboBox, configItemAttribute.Key);
      comboBox.Margin = new Thickness(0, 0, 0, 8);

      var valueBinding = new Binding();
      valueBinding.Source = configItem;
      valueBinding.Path = new PropertyPath(nameof(ConfigurationItemViewModel.Value));
      valueBinding.Mode = BindingMode.TwoWay;
      valueBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
      BindingOperations.SetBinding(comboBox, ComboBox.TextProperty, valueBinding);
      return comboBox;
    }
  }
}