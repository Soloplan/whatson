// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TextWithComboBoxPreselectionConfigControlBuilder.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config
{
  using System.Windows;
  using System.Windows.Controls;
  using MaterialDesignThemes.Wpf;
  using Soloplan.WhatsON.GUI.Config.ViewModel;

  /// <summary>
  /// The control builder for editable combo box with provided suggested values.
  /// </summary>
  /// <seealso cref="Soloplan.WhatsON.GUI.Config.IConfigControlBuilder" />
  public class ComboBoxConfigControlBuilder : ConfigControlBuilder
  {
    /// <summary>
    /// Gets the supported configuration items key.
    /// </summary>
    public override string SupportedConfigurationItemsKey => null;

    /// <summary>
    /// Gets the value binding dependency property.
    /// If set, value binding will be initialized.
    /// </summary>
    public override DependencyProperty ValueBindingDependencyProperty => ComboBox.TextProperty;

    /// <summary>
    /// Creates a new control and returns it.
    /// </summary>
    /// <param name="configItem">The configuration item of the connector.</param>
    /// <param name="configItemAttribute">The configuration item attribute.</param>
    /// <returns>
    /// Returns the <see cref="FrameworkElement" /> for the <see cref="configItem" />.
    /// </returns>
    public override FrameworkElement GetControlInternal(IConfigurationItem configItem, ConfigurationItemAttribute configItemAttribute)
    {
      var comboBox = new ComboBox();
      comboBox.DataContext = configItem;
      var style = Application.Current.FindResource("MaterialDesignFloatingHintComboBox") as Style;
      comboBox.Style = style;
      HintAssist.SetIsFloating(comboBox, true);
      HintAssist.SetHint(comboBox, configItemAttribute.Caption);
      return comboBox;
    }
  }
}