// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigControlBuilder.cs" company="Soloplan GmbH">
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
  /// The base class for all config control builders.
  /// </summary>
  /// <seealso cref="Soloplan.WhatsON.GUI.Config.IConfigControlBuilder" />
  public abstract class ConfigControlBuilder : IConfigControlBuilder
  {
    /// <summary>
    /// Gets the supported configuration items key.
    /// </summary>
    public abstract string SupportedConfigurationItemsKey { get; }

    /// <summary>
    /// Gets the required bottom margin for built control.
    /// </summary>
    public virtual int RequiredBottomMargin { get; } = 13;

    /// <summary>
    /// Gets the value binding dependency property.
    /// If set, value binding will be initialized.
    /// </summary>
    public virtual DependencyProperty ValueBindingDependencyProperty { get; } = null;

    /// <summary>
    /// Creates a new control and returns it.
    /// </summary>
    /// <param name="configItem">The configuration item of the connector.</param>
    /// <param name="configItemAttribute">The configuration item attribute.</param>
    /// <returns>
    /// Returns the <see cref="FrameworkElement" /> for the <see cref="configItem" />.
    /// </returns>
    public FrameworkElement GetControl(IConfigurationItem configItem, ConfigurationItemAttribute configItemAttribute)
    {
      var control = this.GetControlInternal(configItem, configItemAttribute);
      control.Margin = new Thickness(control.Margin.Left, control.Margin.Top, control.Margin.Right, control.Margin.Bottom + this.RequiredBottomMargin);
      if (this.ValueBindingDependencyProperty != null)
      {
        this.InitilizeValueBinding(control, configItem, configItemAttribute);
      }

      return control;
    }

    /// <summary>
    /// Creates a new control and returns it.
    /// </summary>
    /// <param name="configItem">The configuration item.</param>
    /// <param name="configItemAttribute">The configuration item attribute.</param>
    /// <returns>Returns the <see cref="Control" /> for the <see cref="configItem" />.</returns>
    public abstract FrameworkElement GetControlInternal(IConfigurationItem configItem, ConfigurationItemAttribute configItemAttribute);

    /// <summary>
    /// Initilizes the value binding.
    /// </summary>
    /// <param name="control">The control.</param>
    /// <param name="configItem">The configuration item.</param>
    /// <param name="configItemAttribute">The configuration item attribute.</param>
    /// <returns>
    /// The binding instance assigned to the control.
    /// </returns>
    public virtual Binding InitilizeValueBinding(FrameworkElement control, IConfigurationItem configItem, ConfigurationItemAttribute configItemAttribute)
    {
      var valueBinding = new Binding();
      valueBinding.Source = configItem;
      valueBinding.Path = new PropertyPath(nameof(ConfigurationItemViewModel.Value));
      valueBinding.Mode = BindingMode.TwoWay;
      valueBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
      if (!configItemAttribute.Optional)
      {
        var notEmptyValidationRule = new NotEmptyValidationRule();
        notEmptyValidationRule.ValidatesOnTargetUpdated = true;
        notEmptyValidationRule.ValidationStep = ValidationStep.CommittedValue;
        valueBinding.ValidationRules.Add(notEmptyValidationRule);
      }

      this.SetValueBinding(control, this.ValueBindingDependencyProperty, valueBinding);
      return valueBinding;
    }

    /// <summary>
    /// Sets the value binding.
    /// </summary>
    /// <param name="control">The control.</param>
    /// <param name="dependencyProperty">The dependency property.</param>
    /// <param name="valueBinding">The value binding.</param>
    public virtual void SetValueBinding(FrameworkElement control, DependencyProperty dependencyProperty, Binding valueBinding)
    {
      BindingOperations.SetBinding(control, this.ValueBindingDependencyProperty, valueBinding);
    }
  }
}