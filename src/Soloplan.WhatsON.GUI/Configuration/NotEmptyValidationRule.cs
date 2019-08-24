// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NotEmptyValidationRule.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration
{
  using System.Globalization;
  using System.Windows.Controls;
  using System.Windows.Data;
  using Soloplan.WhatsON.GUI.Properties;

  /// <summary>
  /// The validation which checks if non-optional fields are filled.
  /// </summary>
  /// <seealso cref="System.Windows.Controls.ValidationRule" />
  public class NotEmptyValidationRule : ValidationRule
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="NotEmptyValidationRule"/> class.
    /// </summary>
    public NotEmptyValidationRule()
    {
      this.ValidatesOnTargetUpdated = true;
      this.ValidationStep = ValidationStep.CommittedValue;
    }

    /// <summary>
    /// Validates the specified value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="cultureInfo">The culture information.</param>
    /// <returns>True if the <see cref="value"/>is valid.</returns>
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
      if (value is BindingExpression bindingExpression)
      {
        if (bindingExpression.ResolvedSource == null)
        {
          return ValidationResult.ValidResult;
        }

        // because property ValidationStep might be set to ValidationStep.CommittedValue we get BindingExpression as the value and we have to extract the actual value.
        var resolvedType = bindingExpression.ResolvedSource.GetType();
        var prop = resolvedType.GetProperty(bindingExpression.ResolvedSourcePropertyName);
        if (prop == null)
        {
          return new ValidationResult(true, null);
        }

        value = prop.GetValue(bindingExpression.ResolvedSource);
      }

      return string.IsNullOrWhiteSpace((value ?? string.Empty).ToString())
        ? new ValidationResult(false, Resources.ValueIsRequired)
        : ValidationResult.ValidResult;
    }
  }
}