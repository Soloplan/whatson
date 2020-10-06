// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CheckBoxConfigControlBuilder.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration
{
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Controls.Primitives;
  using System.Windows.Data;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common.Converters;

  /// <summary>
  /// The control builder for checkbox.
  /// </summary>
  /// <seealso cref="Soloplan.WhatsON.GUI.Configuration.ConfigControlBuilder" />
  public class CheckBoxConfigControlBuilder : ConfigControlBuilder
  {
    /// <summary>
    /// Gets the supported configuration items key.
    /// </summary>
    public override string SupportedConfigurationItemsKey => null;

    /// <summary>
    /// Gets the value binding dependency property.
    /// If set, value binding will be initialized.
    /// </summary>
    public override DependencyProperty ValueBindingDependencyProperty => ToggleButton.IsCheckedProperty;

    /// <summary>
    /// Creates a new control and returns it.
    /// </summary>
    /// <param name="configItem">The configuration item.</param>
    /// <param name="configItemAttribute">The configuration item attribute.</param>
    /// <returns>
    /// Returns the <see cref="FrameworkElement" /> for the <see cref="configItem" />.
    /// </returns>
    public override FrameworkElement GetControlInternal(IConfigurationItem configItem, ConfigurationItemAttribute configItemAttribute)
    {
      var stackPanel = new StackPanel();
      stackPanel.Orientation = Orientation.Horizontal;
      stackPanel.Margin = new Thickness(0, 0, 0, 0);
      stackPanel.VerticalAlignment = VerticalAlignment.Center;

      var checkBox = new CheckBox();
      checkBox.DataContext = configItem;
      var style = Application.Current.FindResource("MaterialDesignSwitchAccentToggleButton") as Style;
      checkBox.Style = style;
      checkBox.Margin = new Thickness(2, 8, 0, 8);
      stackPanel.Children.Add(checkBox);

      var label = new Label();
      label.Content = configItemAttribute.Caption;
      label.Margin = new Thickness(8, 8, 0, 8);
      label.Padding = new Thickness(0, 0, 0, 0);
      label.VerticalAlignment = VerticalAlignment.Center;
      stackPanel.Children.Add(label);

      return stackPanel;
    }

    /// <summary>
    /// Sets the value binding.
    /// </summary>
    /// <param name="control">The control.</param>
    /// <param name="dependencyProperty">The dependency property.</param>
    /// <param name="valueBinding">The value binding.</param>
    public override void SetValueBinding(FrameworkElement control, DependencyProperty dependencyProperty, Binding valueBinding)
    {
      foreach (var candidateControl in ((StackPanel)control).Children)
      {
        if (candidateControl is CheckBox foundCheckBox)
        {
          valueBinding.Converter = new NullableBooleanConverter();

          BindingOperations.SetBinding(foundCheckBox, this.ValueBindingDependencyProperty, valueBinding);
          return;
        }
      }
    }
  }
}