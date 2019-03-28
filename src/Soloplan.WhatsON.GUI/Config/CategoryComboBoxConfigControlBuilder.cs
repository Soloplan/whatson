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
  using Soloplan.WhatsON.GUI.Config.ViewModel;

  /// <summary>
  /// The control builder for editable combo box with provided suggested values.
  /// </summary>
  /// <seealso cref="Soloplan.WhatsON.GUI.Config.IConfigControlBuilder" />
  public class CategoryComboBoxConfigControlBuilder : ComboBoxConfigControlBuilder
  {
    /// <summary>
    /// Gets the supported configuration items key.
    /// </summary>
    public override string SupportedConfigurationItemsKey => Subject.Category;

    /// <summary>
    /// Creates a new control and returns it.
    /// </summary>
    /// <param name="configItem">The configuration item of the subject.</param>
    /// <param name="configItemAttribute">The configuration item attribute.</param>
    /// <returns>
    /// Returns the <see cref="FrameworkElement" /> for the <see cref="configItem" />.
    /// </returns>
    public override FrameworkElement GetControlInternal (ConfigurationItemViewModel configItem, ConfigurationItemAttribute configItemAttribute)
    {
      var comboBox = (ComboBox)base.GetControlInternal(configItem, configItemAttribute);
      comboBox.IsEditable = true;
      var suggestedValueBinding = new Binding();
      suggestedValueBinding.Source = GlobalConfigDataViewModel.Instance;
      suggestedValueBinding.Path = new PropertyPath(nameof(GlobalConfigDataViewModel.UsedCategories));
      suggestedValueBinding.Mode = BindingMode.OneWay;
      suggestedValueBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
      BindingOperations.SetBinding(comboBox, ItemsControl.ItemsSourceProperty, suggestedValueBinding);
      return comboBox;
    }
  }
}